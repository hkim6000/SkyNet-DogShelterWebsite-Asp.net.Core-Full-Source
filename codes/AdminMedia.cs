using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminMedia : WebPage
    {
        private const string AreaKey = "media";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Media");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--md-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("md-menu", Auth.Menu("md", "AdminMedia", c, site));
            response.SetElementContents("md-who", Auth.Who("md", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminMediaJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"md-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"md-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminMediaJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("md-main", NoAccess("Media"));
                return response;
            }
            response.SetElementContents("md-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminMediaJs.loaded();");
            return response;
        }

        private static string Per(string v)
        {
            return v is "today" or "7" or "30" or "range" ? v : "all";
        }

        private static string Who(Dictionary<string, Staff> staff, string id)
        {
            return id == "web" ? "Website" : staff.TryGetValue(id, out Staff? s) ? s.Name : id;
        }

        private static string Head((string Name, string Cls)[] cols)
        {
            StringBuilder sb = new StringBuilder("<div class=\"md-tr md-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private const int Size = 24;

        private const int ChunkSize = 1024 * 1024;

        private const long MaxVideo = 60L * 1024 * 1024;

        private static string Clock(int s)
        {
            return (s / 60) + ":" + (s % 60).ToString("00", Ui.Inv);
        }

        private static string Bytes(long b)
        {
            return b <= 0 ? string.Empty : b < 1048576 ? Math.Max(1, b / 1024) + " KB" : (b / 1048576.0).ToString(b < 10485760 ? "0.0" : "0", Ui.Inv) + " MB";
        }

        private static List<string> Tags(string v)
        {
            return (v ?? string.Empty).Split(',').Select(t => Regex.Replace(t.Trim().ToLowerInvariant(), "[^a-z0-9 -]", string.Empty).Trim()).Where(t => t.Length > 0).Select(t => t.Length > 24 ? t.Substring(0, 24) : t).Distinct().Take(8).ToList();
        }

        private static string Clean(string v, int max)
        {
            return Regex.Replace(Ui.Clip(v, max), "\\s+", " ").Trim();
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string tagList = Ui.DataList("md-taglist", site.Media.SelectMany(m => m.Tags).Distinct());
            StringBuilder sb = new StringBuilder("<div class=\"md-ptop\"><div><h1>Media</h1><p>Photos and videos for the website. Dogs, Events and Pages pick from this library.</p></div></div>");
            if (Access.Can(c.U, AreaKey, 'E'))
            {
                sb.Append("<details class=\"md-panel md-col\"><summary><b>Upload a photo</b><span>JPG, PNG or WebP &middot; resized to 1600 px</span></summary><div class=\"md-colb\"><div class=\"md-up\"><label class=\"md-drop\" id=\"md-prev\" for=\"md-file\"><span>Choose a photo<br>JPG, PNG or WebP</span><input type=\"file\" id=\"md-file\" accept=\"image/jpeg,image/png,image/webp\" onchange=\"AdminMediaJs.file(this)\"></label><div class=\"md-form\"><label class=\"md-field\"><span>Name</span><input type=\"text\" id=\"md-ptitle\" maxlength=\"80\" placeholder=\"Bear in the garden\"></label><label class=\"md-field\"><span>Description for screen readers</span><input type=\"text\" id=\"md-palt\" maxlength=\"160\" placeholder=\"A black and tan dog sitting on the grass\"></label><label class=\"md-field\"><span>Tags, separated by commas</span><input type=\"text\" id=\"md-ptags\" maxlength=\"200\" list=\"md-taglist\" placeholder=\"dogs, bear\"></label><em class=\"md-err\" id=\"md-e-file\"></em><div><button type=\"button\" class=\"md-btn md-btn-accent\" id=\"md-pbtn\" onclick=\"AdminMediaJs.upload()\">Upload photo</button></div></div></div></div></details>");
                sb.Append("<details class=\"md-panel md-col\"><summary><b>Upload a video</b><span>MP4 or WebM &middot; up to " + (MaxVideo / 1048576) + " MB</span></summary><div class=\"md-colb\"><div class=\"md-up\"><label class=\"md-drop\" id=\"md-vprev\" for=\"md-vfile\"><span>Choose a video<br>MP4 or WebM</span><input type=\"file\" id=\"md-vfile\" accept=\"video/mp4,video/webm\" data-max=\"" + MaxVideo + "\" onchange=\"AdminMediaJs.vpick(this)\"></label><div class=\"md-form\"><label class=\"md-field\"><span>Name</span><input type=\"text\" id=\"md-vtitle\" maxlength=\"80\" placeholder=\"Bear says hello\"></label><label class=\"md-field\"><span>Tags, separated by commas</span><input type=\"text\" id=\"md-vtags\" maxlength=\"200\" list=\"md-taglist\" placeholder=\"dogs, bear\"></label><em class=\"md-err\" id=\"md-e-vfile\"></em><p class=\"md-hint\">Sent in 1 MB parts. The cover picture is taken from the first second. Short, silent clips under 20 MB load best on phones.</p><div class=\"md-prog\" id=\"md-vprog\"><i id=\"md-vbar\"></i></div><div><button type=\"button\" class=\"md-btn md-btn-accent\" id=\"md-vbtn\" onclick=\"AdminMediaJs.vupload()\">Upload video</button></div></div></div></div></details>" + tagList);
            }
            sb.Append("<div class=\"md-mtop\" id=\"md-mchips\">" + Chips(site, string.Empty) + "</div><div class=\"md-bar\"><label>Search<input type=\"search\" id=\"md-q\" list=\"md-qlist\" placeholder=\"Name, tag or description\" maxlength=\"60\" oninput=\"AdminMediaJs.search()\"></label>" + Ui.DataList("md-qlist", site.Media.Select(m => m.Title)) + "<label>Tag<select id=\"md-mtag\" onchange=\"AdminMediaJs.list(1)\"><option value=\"\">Any tag</option>" + Ui.Opts(site.Media.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).Select(t => (t, t)), string.Empty) + "</select></label><label>Used<select id=\"md-muse\" onchange=\"AdminMediaJs.list(1)\"><option value=\"\">Used or not</option><option value=\"used\">Used on the website</option><option value=\"free\">Not used yet</option></select></label></div><div class=\"md-mgrid\" id=\"md-mgrid\"></div><div id=\"md-pager\"></div>");
            return sb.ToString();
        }

        private static string Chips(SiteData site, string cur)
        {
            (string k, string n)[] items = { ("", "All"), ("photo", "Photos"), ("video", "Videos") };
            StringBuilder sb = new StringBuilder("<div class=\"md-chips\">");
            foreach ((string k, string n) in items)
            {
                sb.Append("<button type=\"button\" class=\"md-chip" + (k == cur ? " md-act" : string.Empty) + "\" onclick=\"AdminMediaJs.setKind('" + k + "')\">" + n + "<em>" + (k == string.Empty ? site.Media.Count : site.Media.Count(m => m.Kind == k)) + "</em></button>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private void Redraw(ApiResponse response, Ctx c, SiteData site)
        {
            string kind = GetDataValue("mkind") is "photo" or "video" ? GetDataValue("mkind") : string.Empty;
            string term = Ui.Clip(GetDataValue("mq"), 60).Trim();
            string tag = Ui.Clip(GetDataValue("mtag"), 40);
            string use = GetDataValue("muse");
            Dictionary<string, int> used = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (MediaItem m in site.Media)
            {
                used[m.Full] = Blocks.Uses(site, m.Full).Count;
            }
            List<MediaItem> all = site.Media.Where(m => (kind == string.Empty || m.Kind == kind) && (tag == string.Empty || m.Tags.Contains(tag)) && (use == string.Empty || (use == "used" ? used[m.Full] > 0 : used[m.Full] == 0)) && (term == string.Empty || (m.Title + " " + m.Alt + " " + string.Join(" ", m.Tags)).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderByDescending(m => m.When).ThenBy(m => m.Title).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder();
            if (all.Count == 0)
            {
                sb.Append("<p class=\"md-none\">Nothing matches.</p>");
            }
            foreach (MediaItem m in all.Skip((page - 1) * Size).Take(Size))
            {
                bool vid = m.Kind == "video";
                int n = used[m.Full];
                sb.Append("<div class=\"md-mi\"><button type=\"button\" class=\"md-mthumb\" onclick=\"AdminMediaJs.view('" + Ui.Attr(m.Id) + "')\" aria-label=\"Open " + Ui.Attr(m.Title) + "\"><img src=\"" + Ui.Attr(m.Thumb) + "\" alt=\"\" loading=\"lazy\">" + (vid ? "<span class=\"md-play\"></span><em class=\"md-dur\">" + Clock(m.Seconds) + "</em>" : string.Empty) + "</button><div class=\"md-mtxt\"><b>" + Ui.Enc(m.Title) + "</b><small>" + (vid ? "Video" : "Photo") + (m.Width > 0 ? " &middot; " + m.Width + "&times;" + m.Height : string.Empty) + (m.Bytes > 0 ? " &middot; " + Bytes(m.Bytes) : string.Empty) + "</small><div class=\"md-tags\">" + (n > 0 ? "<span class=\"md-tag md-tag-use\">Used &times;" + n + "</span>" : "<span class=\"md-tag\">Not used</span>") + string.Concat(m.Tags.Take(3).Select(t => "<span class=\"md-tag\">" + Ui.Enc(t) + "</span>")) + "</div></div></div>");
            }
            response.SetElementContents("md-mgrid", sb.ToString());
            response.SetElementContents("md-pager", Ui.Pager("md", page, Size, all.Count, "item", "items", "AdminMediaJs.list"));
            response.SetElementContents("md-mchips", Chips(site, kind));
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            Redraw(response, c, await Store.LoadSite());
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "media", GetDataValue("mid"), "You don't have access to Media.");
            }
            SiteData site = await Store.LoadSite();
            MediaItem? m = site.Media.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 40));
            if (m == null)
            {
                response.ExecuteScript("AdminMediaJs.toast('That item is no longer in Media.');");
                return response;
            }
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            bool vid = m.Kind == "video";
            bool canE = Access.Can(c.U, AreaKey, 'E');
            bool canX = Access.Can(c.U, AreaKey, 'X');
            List<string> uses = Blocks.Uses(site, m.Full);
            string ro = canE ? string.Empty : " readonly";
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(m.Title) + "</h2><div class=\"md-pv\">" + (vid ? "<video controls playsinline preload=\"metadata\" poster=\"" + Ui.Attr(m.Thumb) + "\" src=\"" + Ui.Attr(m.Full) + "\"></video>" : "<img src=\"" + Ui.Attr(m.Full) + "\" alt=\"" + Ui.Attr(m.Alt) + "\">") + "</div>");
            sb.Append("<div class=\"md-form\" style=\"margin-top:16px\"><div class=\"md-f2\"><label class=\"md-field\"><span>Name</span><input type=\"text\" id=\"md-etitle\" maxlength=\"80\" value=\"" + Ui.Attr(m.Title) + "\"" + ro + "></label><label class=\"md-field\"><span>Tags, separated by commas</span><input type=\"text\" id=\"md-etags\" maxlength=\"200\" list=\"md-taglist2\" value=\"" + Ui.Attr(string.Join(", ", m.Tags)) + "\"" + ro + "></label></div>" + Ui.DataList("md-taglist2", site.Media.SelectMany(x => x.Tags).Distinct()) + (vid ? string.Empty : "<label class=\"md-field\"><span>Description for screen readers</span><input type=\"text\" id=\"md-ealt\" maxlength=\"160\" value=\"" + Ui.Attr(m.Alt) + "\"" + ro + " placeholder=\"Describe what is in the photo\"></label>") + "</div>");
            sb.Append("<dl class=\"md-dl\" style=\"margin-top:16px\"><dt>Type</dt><dd>" + (vid ? "Video &middot; " + Clock(m.Seconds) : "Photo") + (m.Width > 0 ? " &middot; " + m.Width + "&times;" + m.Height : string.Empty) + (m.Bytes > 0 ? " &middot; " + Bytes(m.Bytes) : string.Empty) + "</dd><dt>Added</dt><dd>" + Ui.Enc(m.By == string.Empty ? "Sample library" : Ui.StaffName(staff, m.By)) + " &middot; " + m.When.ToString("MMM d, yyyy", Ui.Inv) + "</dd><dt>Used on</dt><dd>" + (uses.Count > 0 ? "<ul class=\"md-uses\">" + string.Concat(uses.Select(u => "<li>" + Ui.Enc(u) + "</li>")) + "</ul>" : "Not used yet") + "</dd><dt>File</dt><dd>" + Ui.Enc(m.Full) + "</dd></dl><div class=\"md-mfoot\">");
            if (canX)
            {
                sb.Append(uses.Count == 0 ? "<button type=\"button\" class=\"md-btn md-btn-danger md-left\" onclick=\"AdminMediaJs.del('" + Ui.Attr(m.Id) + "','" + Ui.Js(m.Title) + "')\">Delete</button>" : "<span class=\"md-hint md-left\">In use, so it cannot be deleted.</span>");
            }
            sb.Append("<button type=\"button\" class=\"md-btn md-btn-ghost\" onclick=\"AdminMediaJs.stopPreview();AdminMediaJs.closeModal()\">" + (canE ? "Cancel" : "Close") + "</button>" + (canE ? "<button type=\"button\" class=\"md-btn md-btn-accent\" onclick=\"AdminMediaJs.save()\">Save</button>" : string.Empty) + "</div>");
            response.SetElementContents("md-mbody", sb.ToString());
            response.ExecuteScript("AdminMediaJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SaveInfo()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "media info", GetDataValue("mid"), "You can look but not change Media.");
            }
            string title = Clean(GetDataValue("mtitle"), 80);
            if (title.Length < 2)
            {
                response.ExecuteScript("AdminMediaJs.toast('Give it a name.');");
                return response;
            }
            SiteData? saved = null;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                MediaItem? m = site.Media.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 40));
                if (m != null)
                {
                    m.Title = title;
                    m.Alt = m.Kind == "photo" ? Clean(GetDataValue("malt"), 160) : string.Empty;
                    m.Tags = Tags(GetDataValue("mtags"));
                    await Store.SaveSite(site);
                    saved = site;
                }
            }
            if (saved == null)
            {
                return response;
            }
            await Store.Log(c.U.Id, "media info", title, true);
            Redraw(response, c, saved);
            response.ExecuteScript("AdminMediaJs.saved();");
            return response;
        }

        private static string ImageExt(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            {
                return "jpg";
            }
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
            {
                return "png";
            }
            if (b.Length > 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            {
                return "webp";
            }
            return string.Empty;
        }

        private static byte[]? DataImage(string data, int max)
        {
            Match m = Regex.Match(data ?? string.Empty, "^data:image/(jpeg|png|webp);base64,([A-Za-z0-9+/=]+)$");
            if (!m.Success)
            {
                return null;
            }
            try
            {
                byte[] b = Convert.FromBase64String(m.Groups[2].Value);
                return b.Length <= max && ImageExt(b) != string.Empty ? b : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string Dir(string sub)
        {
            string d = Path.Combine(Store.Root, "images", "uploads", sub);
            Directory.CreateDirectory(d);
            return d;
        }

        private static (int W, int H) Fit(string w, string h, int max)
        {
            int a = int.TryParse(w, out int av) ? Math.Clamp(av, 0, 20000) : 0;
            int b = int.TryParse(h, out int bv) ? Math.Clamp(bv, 0, 20000) : 0;
            double r = max > 0 ? Math.Min(1.0, (double)max / Math.Max(1, Math.Max(a, b))) : 1.0;
            return ((int)Math.Round(a * r), (int)Math.Round(b * r));
        }

        public async Task<ApiResponse> Upload()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                response.ExecuteScript("AdminMediaJs.upFail('Uploading needs Edit rights on Media.');");
                return await Refuse(response, c, "upload", "photo", "Uploading needs Edit rights on Media.");
            }
            string title = Clean(GetDataValue("mtitle"), 80);
            byte[]? big = DataImage(GetDataValue("full"), 8 * 1024 * 1024);
            byte[]? small = DataImage(GetDataValue("thumb"), 3 * 1024 * 1024);
            if (title.Length < 2)
            {
                response.ExecuteScript("AdminMediaJs.upFail('Give the photo a name.');");
                return response;
            }
            if (big == null || small == null || ImageExt(big) != ImageExt(small))
            {
                response.ExecuteScript("AdminMediaJs.upFail('That file is not a JPG, PNG or WebP photo, or it is too large.');");
                return response;
            }
            string id = Guid.NewGuid().ToString("N").Substring(0, 16);
            string ext = ImageExt(big);
            await File.WriteAllBytesAsync(Path.Combine(Dir("full"), id + "." + ext), big);
            await File.WriteAllBytesAsync(Path.Combine(Dir("thumbs"), id + "." + ext), small);
            (int w, int h) = Fit(GetDataValue("mw"), GetDataValue("mh"), 1600);
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Media.Add(new MediaItem { Id = id, Kind = "photo", Title = title, Alt = Clean(GetDataValue("malt"), 160), Tags = Tags(GetDataValue("mtags")), Full = "images/uploads/full/" + id + "." + ext, Thumb = "images/uploads/thumbs/" + id + "." + ext, Width = w, Height = h, Bytes = big.Length, By = c.U.Id, When = DateTime.UtcNow });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "upload", "photo · " + title, true);
            response.ExecuteScript("AdminMediaJs.uploaded();");
            return response;
        }

        private static string PartFile(string uid)
        {
            return Path.Combine(Store.Root, "temp", "uploads", uid + ".part");
        }

        public async Task<ApiResponse> Chunk()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                response.ExecuteScript("AdminMediaJs.chunkFail('Uploading needs Edit rights on Media.');");
                return await Refuse(response, c, "upload", "video", "Uploading needs Edit rights on Media.");
            }
            string uid = Store.CleanId(GetDataValue("uid"));
            int idx = int.TryParse(GetDataValue("idx"), out int i) ? i : -1;
            int total = int.TryParse(GetDataValue("total"), out int t) ? t : 0;
            if (uid.Length < 16 || idx < 0 || total < 1 || idx >= total || (long)total * ChunkSize > MaxVideo + ChunkSize)
            {
                response.ExecuteScript("AdminMediaJs.chunkFail('The video must be under " + (MaxVideo / 1048576) + " MB.');");
                return response;
            }
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(GetDataValue("part"));
            }
            catch (FormatException)
            {
                response.ExecuteScript("AdminMediaJs.chunkFail('The upload was damaged. Please try again.');");
                return response;
            }
            string file = PartFile(uid);
            string dir = Path.GetDirectoryName(file) ?? Store.Root;
            Directory.CreateDirectory(dir);
            if (idx == 0)
            {
                foreach (string old in Directory.GetFiles(dir, "*.part"))
                {
                    if (File.GetLastWriteTimeUtc(old) < DateTime.UtcNow.AddHours(-2))
                    {
                        File.Delete(old);
                    }
                }
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            long have = File.Exists(file) ? new FileInfo(file).Length : 0;
            if (have != (long)idx * ChunkSize || bytes.Length == 0 || bytes.Length > ChunkSize || (idx < total - 1 && bytes.Length != ChunkSize) || have + bytes.Length > MaxVideo)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                response.ExecuteScript("AdminMediaJs.chunkFail('The upload was interrupted. Please try again.');");
                return response;
            }
            using (FileStream fs = new FileStream(file, FileMode.Append, FileAccess.Write))
            {
                await fs.WriteAsync(bytes);
            }
            response.ExecuteScript("AdminMediaJs.chunkOk(" + idx + ");");
            return response;
        }

        public async Task<ApiResponse> VideoDone()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            string uid = Store.CleanId(GetDataValue("uid"));
            string file = uid.Length >= 16 ? PartFile(uid) : string.Empty;
            if (!Access.Can(c.U, AreaKey, 'E') || file == string.Empty || !File.Exists(file))
            {
                response.ExecuteScript("AdminMediaJs.chunkFail('The upload was interrupted. Please try again.');");
                return response;
            }
            string title = Clean(GetDataValue("mtitle"), 80);
            if (title.Length < 2)
            {
                File.Delete(file);
                response.ExecuteScript("AdminMediaJs.chunkFail('Give the video a name.');");
                return response;
            }
            byte[] head = new byte[12];
            using (FileStream fs = File.OpenRead(file))
            {
                int n = await fs.ReadAsync(head.AsMemory(0, 12));
                if (n < 12)
                {
                    head = Array.Empty<byte>();
                }
            }
            string ext = head.Length == 12 && head[4] == 0x66 && head[5] == 0x74 && head[6] == 0x79 && head[7] == 0x70 ? "mp4" : head.Length == 12 && head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3 ? "webm" : string.Empty;
            if (ext == string.Empty)
            {
                File.Delete(file);
                response.ExecuteScript("AdminMediaJs.chunkFail('That file is not an MP4 or WebM video.');");
                return response;
            }
            string id = Guid.NewGuid().ToString("N").Substring(0, 16);
            long size = new FileInfo(file).Length;
            File.Move(file, Path.Combine(Dir("video"), id + "." + ext));
            string poster = "images/media/video.webp";
            byte[]? still = DataImage(GetDataValue("poster"), 3 * 1024 * 1024);
            if (still != null)
            {
                string pe = ImageExt(still);
                await File.WriteAllBytesAsync(Path.Combine(Dir("thumbs"), id + "-p." + pe), still);
                poster = "images/uploads/thumbs/" + id + "-p." + pe;
            }
            int secs = int.TryParse(GetDataValue("secs"), out int sv) ? Math.Clamp(sv, 0, 36000) : 0;
            (int w, int h) = Fit(GetDataValue("mw"), GetDataValue("mh"), 0);
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Media.Add(new MediaItem { Id = id, Kind = "video", Title = title, Tags = Tags(GetDataValue("mtags")), Full = "images/uploads/video/" + id + "." + ext, Thumb = poster, Seconds = secs, Width = w, Height = h, Bytes = size, By = c.U.Id, When = DateTime.UtcNow });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "upload", "video · " + title, true);
            response.ExecuteScript("AdminMediaJs.vdone();");
            return response;
        }

        public async Task<ApiResponse> Remove()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminMediaJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Refuse(response, c, "delete media", GetDataValue("mid"), "Deleting needs the Delete right on Media.");
            }
            MediaItem? gone = null;
            List<string> uses = new List<string>();
            SiteData? saved = null;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                MediaItem? m = site.Media.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("mid"), 40));
                if (m != null)
                {
                    uses = Blocks.Uses(site, m.Full);
                    if (uses.Count == 0)
                    {
                        site.Media.Remove(m);
                        await Store.SaveSite(site);
                        gone = m;
                        saved = site;
                    }
                }
            }
            if (uses.Count > 0)
            {
                response.ExecuteScript("AdminMediaJs.toast('" + Ui.Js("Used on " + string.Join(", ", uses) + ". Remove it there first.") + "');");
                return response;
            }
            if (gone == null || saved == null)
            {
                return response;
            }
            foreach (string f in new[] { gone.Full, gone.Thumb }.Distinct())
            {
                if (f.StartsWith("images/uploads/", StringComparison.Ordinal) && Ui.SafeMedia(f))
                {
                    File.Delete(Path.Combine(Store.Root, f.Replace('/', Path.DirectorySeparatorChar)));
                }
            }
            await Store.Log(c.U.Id, "delete media", gone.Title, true);
            Redraw(response, c, saved);
            response.ExecuteScript("AdminMediaJs.toast('Deleted.');");
            return response;
        }
    }
}
