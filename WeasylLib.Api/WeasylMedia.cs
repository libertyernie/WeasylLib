using System.Collections.Generic;

namespace WeasylLib.Api {
	public class WeasylMediaFile {
		public int? mediaid;
		public string url;
		public WeasylSubmissionMedia links;
	}

	public class WeasylSubmissionMedia {
		public IEnumerable<WeasylMediaFile> submission;
		public IEnumerable<WeasylMediaFile> thumbnail;
		public IEnumerable<WeasylMediaFile> cover;
	}
}
