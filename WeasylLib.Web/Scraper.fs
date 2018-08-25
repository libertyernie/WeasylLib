namespace WeasylLib.Web

module Scraper =
    open System.Net
    open System.IO
    open System.Text.RegularExpressions

    let AsyncGetCharacterIds user = async {
        if isNull user then
            nullArg "user"
        let req =
            WebUtility.UrlEncode user
            |> sprintf "https://www.weasyl.com/characters/%s"
            |> WebRequest.CreateHttp
        use! resp = req.AsyncGetResponse()
        use sr = new StreamReader(resp.GetResponseStream())
        let! html = sr.ReadToEndAsync() |> Async.AwaitTask
        let regex = new Regex("href=\"/character/([0-9]+)")
        let matches = regex.Matches(html)
        let matchValues = [0..matches.Count] |> Seq.map (fun i -> matches.Item(i).Value)
        return seq {
            for v in matchValues |> Seq.distinct do
                let success, num = System.Int32.TryParse(v)
                if success then yield num
        }
    }