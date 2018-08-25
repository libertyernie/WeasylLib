namespace WeasylLib.Web

open System.Net
open System
open System.Text.RegularExpressions
open System.IO
open System.Threading.Tasks
open System.Text

type WeasylFolder = {
    FolderId: int
    Name: string
}

type WeasylSubmissionType =
    | Sketch = 1010
    | Traditional = 1020
    | Digital = 1030
    | Animation = 1040
    | Photography = 1050
    | Design_Interface = 1060
    | Modeling_Sculpture = 1070
    | Crafts_Jewelry = 1075
    | Sewing_Knitting = 1078
    | Desktop_Wallpaper = 1080
    | Other = 1999

type WeasylRating =
    | General = 10
    | Mature = 30
    | Explicit = 40

type WeasylVisualSubmission = {
    data: byte[]
    contentType: string
    title: string
    submissionType: WeasylSubmissionType
    folderId: Nullable<int>
    rating: WeasylRating
    content: string
    tags: seq<string>
}

type WeasylFrontendClient() =
    let cookieContainer = new CookieContainer()

    let createRequest (url: string) =
        let req = WebRequest.CreateHttp(url)
        req.CookieContainer <- cookieContainer
        req.UserAgent <- "WeasylLib.Web/0.2 (https://https://github.com/libertyernie/WeasylLib)"
        req.CachePolicy <- new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)
        req

    let asyncGetCsrfToken url = async {
        let regex = new Regex("<html[^>]* data-csrf-token=['\"]([A-Za-z0-9]+)['\"]")

        let req = createRequest url
        use! resp = req.AsyncGetResponse()
        use sr = new StreamReader(resp.GetResponseStream())
        let! html = sr.ReadToEndAsync() |> Async.AwaitTask
        let m = regex.Match(html)
        if m.Success then
            return m.Groups.[1].Value
        else
            return failwithf "Cross-site request forgery prevention token not found on %s" url
    }

    let asyncGetSignoutUrl = async {
        let regex = new Regex("/signout\\?token=[A-Za-z0-9]+")
        let url = "https://www.weasyl.com/"

        let req = createRequest url
        use! resp = req.AsyncGetResponse()
        use sr = new StreamReader(resp.GetResponseStream())
        let! html = sr.ReadToEndAsync() |> Async.AwaitTask
        let m = regex.Match(html)
        if m.Success then
            return Some <| sprintf "https://www.weasyl.com%s" m.Value
        else
            return None
    }

    member __.WZL
        with get () =
            let cookies = "https://www.weasyl.com" |> Uri |> cookieContainer.GetCookies
            let wzl = cookies.["WZL"]
            if isNull wzl
                then null
                else wzl.Value
        and set (value) =
            let cookieValue = Regex.Replace(value, "[^A-Za-z0-9]", "")
            let header = sprintf "WZL=%s" cookieValue
            cookieContainer.SetCookies("https://www.weasyl.com" |> Uri, header)

    member __.AsyncSignIn username password sfw = async {
        let url = "https://www.weasyl.com/signin"
        let! token = asyncGetCsrfToken url

        let req = createRequest url
        req.Method <- "POST"
        req.ContentType <- "application/x-www-form-urlencoded"

        do! async {
            use! reqStream = req.GetRequestStreamAsync() |> Async.AwaitTask
            use sw = new StreamWriter(reqStream)

            let w = WebUtility.UrlEncode >> sw.WriteLineAsync >> Async.AwaitTask

            do! w <| sprintf "token=%s&" token
            do! w <| sprintf "username=%s&" username
            do! w <| sprintf "password=%s&" password
            do! w <| sprintf "sfwmode=%s&" (if sfw then "sfw" else "nsfw")
            do! w "referer=https://www.weasyl.com/"
        }

        use! resp = req.AsyncGetResponse()
        ignore resp

        return ()
    }

    member __.AsyncGetUsername = async {
        let regex = new Regex("<a id=['\"]username['\"][^>]* href=['\"]/~([^'\"]+)")
        let url = "https://www.weasyl.com/"

        let req = createRequest url
        use! resp = req.AsyncGetResponse()
        use sr = new StreamReader(resp.GetResponseStream())
        let! html = sr.ReadToEndAsync() |> Async.AwaitTask
        let m = regex.Match(html)
        if m.Success then
            return Some m.Groups.[1].Value
        else
            return None
    }

    member __.AsyncGetFolders = async {
        let req = createRequest "https://www.weasyl.com/submit/visual"
        use! resp = req.AsyncGetResponse()
        use sr = new StreamReader(resp.GetResponseStream())
        let! html = sr.ReadToEndAsync() |> Async.AwaitTask

        return seq {
            let section = Regex.Match(html, "<select name=\"folderid\".*?</select>", RegexOptions.Singleline)
            if section.Success then
                let lines = Regex.Split(section.Value, @"\r?\n|\r")

                let regex = new Regex(@"<option value=""(\d+)"">([^<]+)</option>")
                for line in lines do
                    let m = regex.Match(line)
                    if m.Success then
                        yield {
                            FolderId = Int32.Parse(m.Groups.[1].Value)
                            Name = m.Groups.[2].Value
                        }
        }
    }

    member __.AsyncUploadVisual (submission: WeasylVisualSubmission) = async {
        let ext = Seq.last (submission.contentType.Split('/'))
        let filename = sprintf "file.%s" ext

        let! token = asyncGetCsrfToken "https://www.weasyl.com/submit/visual"

        // multipart separators
        let h1 = sprintf "-----------------------------%d" DateTime.UtcNow.Ticks
        let h2 = sprintf "--%s" h1
        let h3 = sprintf "--%s--" h1

        let req = createRequest "https://www.weasyl.com/submit/visual"
        req.Method <- "POST"
        req.ContentType <- sprintf "multipart/form-data; boundary=%s" h1

        do! async {
            use ms = new MemoryStream()
            let w (s: string) =
                let bytes = Encoding.UTF8.GetBytes(sprintf "%s\n" s)
                ms.Write(bytes, 0, bytes.Length)
            
            w h2
            w "Content-Disposition: form-data; name=\"token\""
            w ""
            w token
            w h2
            w (sprintf "Content-Disposition: form-data; name=\"submitfile\"; filename=\"%s\"" filename)
            w (sprintf "Content-Type: %s" submission.contentType)
            w ""
            ms.Flush()
            ms.Write(submission.data, 0, submission.data.Length)
            ms.Flush()
            w ""
            w h2
            w "Content-Disposition: form-data; name=\"thumbfile\"; filename=\"\""
            w "Content-Type: application/octet-stream"
            w ""
            w ""
            w h2
            w "Content-Disposition: form-data; name=\"title\""
            w ""
            w submission.title
            w h2
            w "Content-Disposition: form-data; name=\"subtype\""
            w ""
            w (submission.submissionType.ToString("d"))
            w h2
            w "Content-Disposition: form-data; name=\"folderid\""
            w ""
            w (submission.folderId.ToString())
            w h2
            w "Content-Disposition: form-data; name=\"rating\""
            w ""
            w (submission.rating.ToString("d"))
            w h2
            w "Content-Disposition: form-data; name=\"content\""
            w ""
            w submission.content
            w h2
            w "Content-Disposition: form-data; name=\"tags\""
            w ""
            w (submission.tags |> Seq.map (fun s -> s.Replace(' ', '_')) |> String.concat " ")
            w h3

            use! reqStream = req.GetRequestStreamAsync() |> Async.AwaitTask
            ms.Position <- 0L
            do! ms.CopyToAsync(reqStream) |> Async.AwaitTask
        }

        return! async {
            use! resp = req.AsyncGetResponse()
            return resp.ResponseUri
        }
    }

    member __.AsyncSignOut = async {
        let! uri = asyncGetSignoutUrl
        match uri with
        | Some u ->
            let req = createRequest u
            let! resp = req.AsyncGetResponse()
            ignore resp
        | None -> ()
    }

    member this.SignInAsync(username, password, sfw) =
        this.AsyncSignIn username password sfw |> Async.StartAsTask :> Task
    member this.GetUsernameAsync() =
        async {
            let! str = this.AsyncGetUsername
            return str |> Option.defaultValue null
        }|> Async.StartAsTask
    member this.GetFoldersAsync() =
        this.AsyncGetFolders |> Async.StartAsTask
    member this.UploadVisualAsync(submission) =
        this.AsyncUploadVisual submission |> Async.StartAsTask
    member this.SignOutAsync() =
        this.AsyncSignOut |> Async.StartAsTask :> Task