using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminDogs : WebPage
    {
        private const string AreaKey = "dogs";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Dogs");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--dz-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("dz-menu", Auth.Menu("dz", "AdminDogs", c, site));
            response.SetElementContents("dz-who", Auth.Who("dz", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminDogsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"dz-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"dz-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminDogsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("dz-main", NoAccess("Dogs"));
                return response;
            }
            response.SetElementContents("dz-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminDogsJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"dz-tr dz-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string PickItem(MediaItem m)
        {
            bool vid = m.Kind == "video";
            return "<button type=\"button\" class=\"dz-pki\" data-src=\"" + Ui.Attr(m.Full) + "\" data-thumb=\"" + Ui.Attr(m.Thumb) + "\" data-kind=\"" + m.Kind + "\" data-title=\"" + Ui.Attr(m.Title) + "\" onclick=\"AdminDogsJs.pkToggle(this)\" title=\"" + Ui.Attr(m.Title + (m.Tags.Count > 0 ? " · " + string.Join(", ", m.Tags) : string.Empty)) + "\"><img src=\"" + Ui.Attr(m.Thumb) + "\" alt=\"\" loading=\"lazy\">" + (vid ? "<b class=\"dz-vbadge\">" + (m.Seconds / 60) + ":" + (m.Seconds % 60).ToString("00", Ui.Inv) + "</b>" : string.Empty) + "<span>" + Ui.Enc(m.Title) + "</span><i>&#10003;</i></button>";
        }

        public async Task<ApiResponse> MediaPick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            string allow = GetDataValue("pallow") is "photo" or "video" ? GetDataValue("pallow") : string.Empty;
            string kind = allow != string.Empty ? allow : GetDataValue("pkind") is "photo" or "video" ? GetDataValue("pkind") : string.Empty;
            string term = Ui.Clip(GetDataValue("pterm"), 60).Trim();
            string tag = Ui.Clip(GetDataValue("ptag"), 40);
            List<string> ctx = Ui.Clip(GetDataValue("pctx"), 80).ToLowerInvariant().Split(',').Select(x => x.Trim()).Where(x => x != string.Empty).ToList();
            int page = int.TryParse(GetDataValue("ppg"), out int pv) ? Math.Clamp(pv, 1, 200) : 1;
            const int size = 30;
            List<MediaItem> all = site.Media.Where(m => (kind == string.Empty || m.Kind == kind) && (tag == string.Empty || m.Tags.Contains(tag)) && (term == string.Empty || (m.Title + " " + m.Alt + " " + string.Join(" ", m.Tags)).Contains(term, StringComparison.OrdinalIgnoreCase))).OrderByDescending(m => m.Tags.Count(t => ctx.Contains(t))).ThenByDescending(m => m.When).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (MediaItem m in all.Skip((page - 1) * size).Take(size))
            {
                sb.Append(PickItem(m));
            }
            if (all.Count == 0)
            {
                sb.Append("<p class=\"dz-none\" style=\"grid-column:1/-1\">Nothing matches. Upload a photo, or add videos in Media.</p>");
            }
            string tags = page == 1 ? JsonSerializer.Serialize(site.Media.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).ToList()) : "null";
            response.ExecuteScript("AdminDogsJs.pkShown(" + JsonSerializer.Serialize(sb.ToString()) + "," + (all.Count > page * size ? "true" : "false") + "," + tags + "," + (page > 1 ? "true" : "false") + ");");
            return response;
        }

        private static string PickExt(byte[] b)
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

        private static byte[]? PickData(string data, int max)
        {
            Match m = Regex.Match(data ?? string.Empty, "^data:image/(jpeg|png|webp);base64,([A-Za-z0-9+/=]+)$");
            if (!m.Success)
            {
                return null;
            }
            try
            {
                byte[] b = Convert.FromBase64String(m.Groups[2].Value);
                return b.Length <= max && PickExt(b) != string.Empty ? b : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public async Task<ApiResponse> MediaUp()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "upload", "photo", "You can look but not upload.");
            }
            byte[]? big = PickData(GetDataValue("full"), 8 * 1024 * 1024);
            byte[]? small = PickData(GetDataValue("thumb"), 3 * 1024 * 1024);
            if (big == null || small == null || PickExt(big) != PickExt(small))
            {
                response.ExecuteScript("AdminDogsJs.pkUploaded(false,'That file is not a JPG, PNG or WebP photo, or it is too large.');");
                return response;
            }
            string ext = PickExt(big);
            string id = Guid.NewGuid().ToString("N").Substring(0, 16);
            string dir = Path.Combine(Store.Root, "images", "uploads");
            Directory.CreateDirectory(Path.Combine(dir, "full"));
            Directory.CreateDirectory(Path.Combine(dir, "thumbs"));
            await File.WriteAllBytesAsync(Path.Combine(dir, "full", id + "." + ext), big);
            await File.WriteAllBytesAsync(Path.Combine(dir, "thumbs", id + "." + ext), small);
            string title = Regex.Replace(Ui.Clip(GetDataValue("pname"), 80), "\\s+", " ").Trim();
            List<string> ctx = Ui.Clip(GetDataValue("pctx"), 80).ToLowerInvariant().Split(',').Select(x => Regex.Replace(x, "[^a-z0-9 -]", string.Empty).Trim()).Where(x => x != string.Empty).Distinct().Take(4).ToList();
            int w = int.TryParse(GetDataValue("pw"), out int wv) ? Math.Clamp(wv, 0, 20000) : 0;
            int h = int.TryParse(GetDataValue("ph"), out int hv) ? Math.Clamp(hv, 0, 20000) : 0;
            double r = Math.Min(1.0, 1600.0 / Math.Max(1, Math.Max(w, h)));
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                site.Media.Add(new MediaItem { Id = id, Kind = "photo", Title = title.Length >= 2 ? title : "Photo " + id.Substring(0, 6), Tags = ctx, Full = "images/uploads/full/" + id + "." + ext, Thumb = "images/uploads/thumbs/" + id + "." + ext, Width = (int)Math.Round(w * r), Height = (int)Math.Round(h * r), Bytes = big.Length, By = c.U.Id, When = DateTime.UtcNow });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "upload", "photo · " + title, true);
            response.ExecuteScript("AdminDogsJs.pkUploaded(true,'');");
            return response;
        }

        private const int Size = 20;

        private static readonly (string Key, string Name)[] SortOpts = { ("new", "Newest intake"), ("longest", "Waiting longest"), ("name", "Name, A to Z"), ("views", "Most viewed"), ("young", "Youngest first") };

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string[] a = (arg ?? string.Empty).Split('|');
            string st = Ui.DogStatuses.Any(s => s.Key == a[0]) ? a[0] : string.Empty;
            string so = a.Length > 1 && SortOpts.Any(s => s.Key == a[1]) ? a[1] : "new";
            StringBuilder sb = new StringBuilder("<div class=\"dz-ptop\"><div><h1>Dogs</h1><p>Every dog in our care. Only Available, Adoption pending and In foster dogs show on the website.</p></div><div class=\"dz-acts\">" + (Access.Can(c.U, AreaKey, 'E') ? "<button type=\"button\" class=\"dz-btn dz-btn-accent\" onclick=\"AdminDogsJs.edit('')\">Add a dog</button>" : string.Empty) + "</div></div>");
            sb.Append("<div class=\"dz-bar\"><label>Search<input type=\"search\" id=\"dz-q\" list=\"dz-qlist\" placeholder=\"Name, breed or ID\" maxlength=\"40\" oninput=\"AdminDogsJs.search()\"></label>" + Ui.DataList("dz-qlist", site.Dogs.Select(d => d.Name).Concat(site.Dogs.Select(d => d.Breed))));
            sb.Append("<label>Status<select id=\"dz-fstat\" onchange=\"AdminDogsJs.list(1)\"><option value=\"\">In our care</option>" + Ui.Opts(Ui.DogStatuses.Select(s => (s.Key, s.Name)), st) + "<option value=\"all\">Everything</option></select></label>");
            sb.Append("<label>Size<select id=\"dz-fsize\" onchange=\"AdminDogsJs.list(1)\"><option value=\"\">Any size</option>" + Ui.Opts(Ui.Sizes.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>Sex<select id=\"dz-fsex\" onchange=\"AdminDogsJs.list(1)\"><option value=\"\">Both</option>" + Ui.Opts(new[] { ("female", "Female"), ("male", "Male") }, string.Empty) + "</select></label>");
            sb.Append("<label>Sort<select id=\"dz-fsort\" onchange=\"AdminDogsJs.list(1)\">" + Ui.Opts(SortOpts.Select(s => (s.Key, s.Name)), so) + "</select></label></div><div class=\"dz-tbl dz-dztbl\" id=\"dz-list\"></div><div id=\"dz-pager\"></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            string term = Ui.Clip(GetDataValue("dq"), 40);
            string st = GetDataValue("dstat");
            string size = GetDataValue("dsize");
            string sex = GetDataValue("dsex");
            IEnumerable<Pet> q = site.Dogs.Where(d => (st == "all" || (st == string.Empty ? d.Status != "adopted" : d.Status == st)) && (size == string.Empty || d.Size == size) && (sex == string.Empty || d.Sex == sex));
            if (term != string.Empty)
            {
                q = q.Where(d => (d.Name + " " + d.Breed + " " + d.Id + " " + d.Color).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            q = GetDataValue("dsort") switch
            {
                "longest" => q.OrderBy(d => d.Intake),
                "name" => q.OrderBy(d => d.Name),
                "views" => q.OrderByDescending(d => d.Views),
                "young" => q.OrderByDescending(d => d.Born),
                _ => q.OrderByDescending(d => d.Intake)
            };
            List<Pet> all = q.ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { (string.Empty, string.Empty), ("Dog", string.Empty), ("Breed", string.Empty), ("Size", string.Empty), ("Status", string.Empty), ("In care", "dz-num"), ("Views", "dz-num"), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"dz-tr\"><span class=\"dz-none dz-wide\">No dogs match.</span></div>");
            }
            foreach (Pet d in all.Skip((page - 1) * Size).Take(Size))
            {
                string img = d.Photos.Count > 0 ? "<img class=\"dz-thumb\" src=\"" + Ui.Attr(Ui.Thumb(Ui.Cover(site, d))) + "\" alt=\"\" loading=\"lazy\">" : "<span class=\"dz-hint\">No photo</span>";
                int days = Math.Max(0, ((d.Adopted ?? today) - d.Intake.Date).Days);
                string flags = (d.Featured ? "<span class=\"dz-flag\">Featured</span>" : string.Empty) + (d.Urgent ? "<span class=\"dz-flag dz-urg\">Urgent</span>" : string.Empty) + (d.Bonded != string.Empty ? "<span class=\"dz-flag\">Bonded</span>" : string.Empty);
                sb.Append("<div class=\"dz-tr\"><span>" + img + "</span><span class=\"dz-wide\"><b>" + Ui.Enc(d.Name) + "</b><small>" + Ui.Enc(d.Id) + " &middot; " + Ui.SexName(d.Sex) + " &middot; " + Ui.Age(d, today) + "</small></span><span data-l=\"Breed\">" + Ui.Enc(d.Breed) + "</span><span data-l=\"Size\">" + Ui.SizeName(d.Size) + "<small>" + d.Weight + " lb</small></span><span data-l=\"Status\">" + Ui.Pill("dz", Ui.DogStatuses, d.Status) + (flags != string.Empty ? "<span class=\"dz-flags\">" + flags + "</span>" : string.Empty) + "</span><span data-l=\"In care\" class=\"dz-num\">" + Ui.Plural(days, "day", "days") + "</span><span data-l=\"Views\" class=\"dz-num\">" + Ui.Num(d.Views) + "<small>" + Ui.Num(d.Saves) + " saves</small></span><span><button type=\"button\" class=\"dz-btn dz-btn-sm dz-btn-ghost\" onclick=\"AdminDogsJs.edit('" + Ui.Attr(d.Id) + "')\">" + (Access.Can(c.U, AreaKey, 'E') ? "Edit" : "View") + "</button></span></div>");
            }
            response.SetElementContents("dz-list", sb.ToString());
            response.SetElementContents("dz-pager", Ui.Pager("dz", page, Size, all.Count, "dog", "dogs", "AdminDogsJs.list"));
            return response;
        }

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"dz-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"dz-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private static string S(string id, string label, IEnumerable<(string, string)> opts, string cur, string extra = "")
        {
            return "<label class=\"dz-field\"><span>" + label + "</span><select id=\"dz-" + id + "\"" + extra + ">" + Ui.Opts(opts, cur) + "</select></label>";
        }

        private static string Chk(string id, string label, bool on, bool enabled = true)
        {
            return "<label class=\"dz-chk\"><input type=\"checkbox\" id=\"dz-" + id + "\"" + (on ? " checked" : string.Empty) + (enabled ? string.Empty : " disabled") + "> " + label + "</label>";
        }

        private static string HealthRow(string date, string kind, string note)
        {
            return "<div class=\"dz-hrow2\"><input type=\"date\" value=\"" + Ui.Attr(date) + "\" aria-label=\"Date\"><select aria-label=\"Kind\">" + Ui.Opts(Ui.HealthKinds.Select(k => (k.Key, k.Name)), kind) + "</select><input type=\"text\" maxlength=\"160\" value=\"" + Ui.Attr(note) + "\" placeholder=\"Rabies, 3-year\" aria-label=\"Note\"><button type=\"button\" class=\"dz-ibtn\" aria-label=\"Remove\" onclick=\"AdminDogsJs.dropRow(this)\">&times;</button></div>";
        }

        public async Task<ApiResponse> Form()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "dog", GetDataValue("did"), "You don't have access to dogs.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            Pet? found = site.Dogs.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("did"), 20));
            if (found == null && !Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "dog", "new", "You can look but not add dogs.");
            }
            bool canE = Access.Can(c.U, AreaKey, 'E');
            bool canX = Access.Can(c.U, AreaKey, 'X');
            Pet d = found ?? new Pet { Status = "intake", Sex = "female", Size = "medium", Energy = "medium", Intake = today, Born = today.AddYears(-2), Fee = site.Settings.FeeAdult };
            string ro = canE ? string.Empty : " disabled";
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            StringBuilder sb = new StringBuilder("<h2>" + (found == null ? "Add a dog" : Ui.Enc(d.Name) + " <small style=\"font-family:var(--dz-text);font-size:14px;color:var(--dz-ink2)\">" + Ui.Enc(d.Id) + "</small>") + "</h2><div id=\"dz-ddata\" data-id=\"" + Ui.Attr(found?.Id ?? string.Empty) + "\" data-photos=\"" + Ui.Attr(JsonSerializer.Serialize(d.Photos)) + "\" data-thumbs=\"" + Ui.Attr(JsonSerializer.Serialize(d.Photos.Distinct().ToDictionary(p => p, p => Ui.Poster(site, p)))) + "\" hidden></div>");
            sb.Append("<div class=\"dz-tabs\" role=\"tablist\"><button type=\"button\" class=\"dz-tab dz-act\" data-p=\"det\" onclick=\"AdminDogsJs.pane('det')\">Details</button><button type=\"button\" class=\"dz-tab\" data-p=\"txt\" onclick=\"AdminDogsJs.pane('txt')\">Story</button><button type=\"button\" class=\"dz-tab\" data-p=\"pho\" onclick=\"AdminDogsJs.pane('pho')\">Photos<em id=\"dz-pcount\">" + d.Photos.Count + "</em></button><button type=\"button\" class=\"dz-tab\" data-p=\"hea\" onclick=\"AdminDogsJs.pane('hea')\">Health<em>" + d.Health.Count + "</em></button>" + (d.History.Count > 0 ? "<button type=\"button\" class=\"dz-tab\" data-p=\"his\" onclick=\"AdminDogsJs.pane('his')\">History</button>" : string.Empty) + "</div>");
            sb.Append("<fieldset class=\"dz-form\" id=\"dz-dform\" style=\"border:0;padding:0;margin:0;min-width:0\"" + ro + "><div class=\"dz-panes\">");
            sb.Append("<div data-p=\"det\" class=\"dz-act\"><div class=\"dz-f3\">" + F("dname", "Name", d.Name, 30) + F("dbreed", "Breed", d.Breed, 50, "text", " list=\"dz-breeds\"") + Ui.DataList("dz-breeds", site.Dogs.Select(x => x.Breed)) + S("dsex", "Sex", new[] { ("female", "Female"), ("male", "Male") }, d.Sex) + "</div>");
            sb.Append("<div class=\"dz-f4\">" + F("dborn", "Born (about)", Ui.Ymd(d.Born), 10, "date") + S("dsize", "Size", Ui.Sizes.Select(x => (x.Key, x.Name + " (" + Ui.NameOf(Ui.SizeHints, x.Key) + ")")), d.Size) + F("dweight", "Weight (lb)", d.Weight > 0 ? d.Weight.ToString(Ui.Inv) : string.Empty, 3, "text", " inputmode=\"numeric\"") + F("dcolor", "Color", d.Color, 40) + "</div>");
            sb.Append("<div class=\"dz-f4\">" + S("denergy", "Energy", Ui.Energies.Select(x => (x.Key, x.Name)), d.Energy) + S("dkids", "Good with kids", Ui.GoodWith.Select(x => (x.Key, x.Name)), d.Kids) + S("ddogs", "Good with dogs", Ui.GoodWith.Select(x => (x.Key, x.Name)), d.Dogs) + S("dcats", "Good with cats", Ui.GoodWith.Select(x => (x.Key, x.Name)), d.Cats) + "</div>");
            sb.Append("<div class=\"dz-checks\">" + Chk("dhouse", "House-trained", d.HouseTrained) + Chk("dvacc", "Vaccinated", d.Vaccinated) + Chk("dfixed", "Spayed or neutered", d.Fixed) + Chk("dchip", "Microchipped", d.Chipped) + "</div>");
            sb.Append("<div class=\"dz-f4\">" + F("dfee", "Adoption fee ($)", d.Fee.ToString("0.##", Ui.Inv), 7, "text", " inputmode=\"decimal\"") + "<label class=\"dz-field\"><span>Bonded with</span><select id=\"dz-dbonded\"><option value=\"\">Nobody</option>" + Ui.Opts(site.Dogs.Where(x => x.Id != d.Id && x.Status != "adopted").OrderBy(x => x.Name).Select(x => (x.Id, x.Name + " (" + x.Id + ")")), d.Bonded) + "</select></label>" + F("dintake", "Intake date", Ui.Ymd(d.Intake), 10, "date") + S("dstatus", "Status", Ui.DogStatuses.Select(x => (x.Key, x.Name)), d.Status, (canX ? string.Empty : " disabled") + " onchange=\"AdminDogsJs.statView()\"") + "</div>");
            sb.Append("<div class=\"dz-f2 dz-adopt\">" + F("dadopter", "Adopted by", d.Adopter, 60) + F("dadopted", "Adopted on", d.Adopted != null ? Ui.Ymd(d.Adopted.Value) : Ui.Ymd(today), 10, "date") + "</div>");
            sb.Append(F("dneeds", "Care notes (shown on the website)", d.Needs, 160, "text", " placeholder=\"Ear drops twice a day\""));
            sb.Append("<div class=\"dz-checks\">" + Chk("dfeat", "Featured on the home page", d.Featured, canX) + Chk("durgent", "Urgent: needs a home soon", d.Urgent, canX) + "</div>" + (canX ? string.Empty : "<p class=\"dz-lock\">Status, Featured and Urgent need the Publish right. New dogs start in Intake.</p>") + "</div>");
            sb.Append("<div data-p=\"txt\"><label class=\"dz-field\"><span>Story (leave an empty line between paragraphs)</span><textarea id=\"dz-dstory\" rows=\"10\" maxlength=\"3000\">" + Ui.Enc(d.Story) + "</textarea></label><label class=\"dz-field\"><span>Personality (one per line, up to 6)</span><textarea id=\"dz-dtraits\" rows=\"4\" maxlength=\"400\">" + Ui.Enc(string.Join("\n", d.Traits)) + "</textarea></label><label class=\"dz-field\"><span>Happy tail (shown on the Stories page after adoption)</span><textarea id=\"dz-dtail\" rows=\"4\" maxlength=\"800\">" + Ui.Enc(d.HappyTail) + "</textarea></label></div>");
            sb.Append("<div data-p=\"pho\"><p class=\"dz-hint\">The first photo is the cover on cards. Videos show in the gallery with a play button. Use the arrows to change the order.</p><div class=\"dz-photos\" id=\"dz-photos\"></div>" + (canE ? "<div><button type=\"button\" class=\"dz-btn dz-btn-sm\" onclick=\"AdminDogsJs.addMedia()\">Add from Media</button></div><p class=\"dz-hint\">Upload new photos from the picker, or videos in <a href=\"AdminMedia\">Media</a>.</p>" : string.Empty));
            sb.Append("</div><div data-p=\"hea\"><p class=\"dz-hint\">Vaccines, vet visits, medicine and surgery. Only the staff see this list.</p>" + (canE ? "<div><button type=\"button\" class=\"dz-btn dz-btn-sm dz-btn-ghost\" onclick=\"AdminDogsJs.addHealth()\">Add a health record</button></div>" : string.Empty) + "<div id=\"dz-health\" style=\"display:grid;gap:8px\">");
            foreach (HealthEvent h in d.Health.OrderByDescending(h => h.When))
            {
                sb.Append(HealthRow(Ui.Ymd(h.When), h.Kind, h.Note));
            }
            sb.Append("</div><template id=\"dz-htpl\">" + HealthRow(Ui.Ymd(today), "vet", string.Empty) + "</template></div>");
            if (d.History.Count > 0)
            {
                sb.Append("<div data-p=\"his\"><div class=\"dz-hist\">");
                foreach (DogEvent h in d.History.OrderByDescending(h => h.When))
                {
                    sb.Append("<div><span>" + h.When.ToString("MMM d, yyyy", Ui.Inv) + " &middot; " + Ui.Enc(h.Event) + "</span><span class=\"dz-hint\">" + Ui.Enc(Ui.StaffName(staff, h.By)) + "</span></div>");
                }
                sb.Append("</div><p class=\"dz-hint\">" + Ui.Num(d.Views) + " page views &middot; " + Ui.Num(d.Saves) + " saves. Status changes are added here when you save.</p></div>");
            }
            sb.Append("</div></fieldset><em class=\"dz-err\" id=\"dz-e-save\" role=\"alert\"></em><div class=\"dz-mfoot\">");
            if (found != null && canX)
            {
                sb.Append("<button type=\"button\" class=\"dz-btn dz-btn-danger dz-left\" onclick=\"AdminDogsJs.remove('" + Ui.Attr(found.Id) + "','" + Ui.Js(found.Name) + "')\">Delete</button>");
            }
            if (found != null && (Ui.Public(found) || found.Status == "adopted"))
            {
                sb.Append("<a class=\"dz-btn dz-btn-ghost\" href=\"Dog?id=" + Uri.EscapeDataString(found.Id) + "\" target=\"_blank\" rel=\"noopener\">Open on the website</a>");
            }
            sb.Append("<button type=\"button\" class=\"dz-btn dz-btn-ghost\" onclick=\"AdminDogsJs.closeModal()\">" + (canE ? "Cancel" : "Close") + "</button>" + (canE ? "<button type=\"button\" class=\"dz-btn dz-btn-accent\" onclick=\"AdminDogsJs.save()\">" + (found == null ? "Add dog" : "Save") + "</button>" : string.Empty) + "</div>");
            response.SetElementContents("dz-mbody", sb.ToString());
            response.ExecuteScript("AdminDogsJs.opened();");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "dog", GetDataValue("did"), "You can look but not change dogs.");
            }
            string id = Ui.Clip(GetDataValue("did"), 20);
            bool canX = Access.Can(c.U, AreaKey, 'X');
            string name = Regex.Replace(Ui.Clip(GetDataValue("dname"), 30), "\\s+", " ").Trim();
            string breed = Regex.Replace(Ui.Clip(GetDataValue("dbreed"), 50), "\\s+", " ").Trim();
            DateTime? born = Ui.ParseDay(GetDataValue("dborn"));
            DateTime? intake = Ui.ParseDay(GetDataValue("dintake"));
            decimal fee = decimal.TryParse(Regex.Replace(GetDataValue("dfee"), "[$,\\s]", string.Empty), NumberStyles.Number, Ui.Inv, out decimal fv) ? Math.Clamp(Math.Round(fv, 2), 0, 10000) : 0;
            int weight = int.TryParse(GetDataValue("dweight"), NumberStyles.Integer, Ui.Inv, out int wv) ? Math.Clamp(wv, 0, 300) : 0;
            List<string> photos = GetDataValue("dphotos").Split('|').Where(p => p != string.Empty && Ui.SafeMedia(p)).Distinct().Take(12).ToList();
            List<HealthEvent> health = new List<HealthEvent>();
            foreach (string row in GetDataValue("dhealth").Split('~'))
            {
                string[] x = row.Split('|');
                DateTime? hd = x.Length == 3 ? Ui.ParseDay(x[0]) : null;
                if (hd != null && Ui.HealthKinds.Any(k => k.Key == x[1]) && x[2].Trim() != string.Empty)
                {
                    health.Add(new HealthEvent { When = hd.Value, Kind = x[1], Note = Ui.Clip(x[2], 160) });
                }
            }
            string err = string.Empty;
            string tab = "det";
            string want = Ui.DogStatuses.Any(s => s.Key == GetDataValue("dstatus")) ? GetDataValue("dstatus") : string.Empty;
            if (name.Length < 2)
            {
                err = "Enter the dog's name.";
            }
            else if (breed.Length < 2)
            {
                err = "Enter the breed, for example “Labrador mix”.";
            }
            else if (born == null || born.Value > DateTime.UtcNow.Date.AddDays(1) || born.Value < DateTime.UtcNow.Date.AddYears(-25))
            {
                err = "Check the birth date. An estimate is fine.";
            }
            else if (intake == null)
            {
                err = "Enter the intake date.";
            }
            else if (want == "adopted" && canX && Ui.Clip(GetDataValue("dadopter"), 60).Trim().Length < 2)
            {
                err = "Enter who adopted the dog.";
            }
            else if (canX && want != string.Empty && want != "intake" && want != "medical" && want != "adopted" && !photos.Any(p => !Ui.IsVideo(p)))
            {
                err = "Add at least one photo before the dog goes on the website.";
                tab = "pho";
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminDogsJs.fail('" + Ui.Js(err) + "','" + tab + "');");
                return response;
            }
            bool isNew = false;
            string saved = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                Pet? d = site.Dogs.FirstOrDefault(x => x.Id == id);
                if (d == null)
                {
                    isNew = true;
                    d = new Pet { Id = Store.NextId(site.Dogs.Select(x => x.Id), "D-", 100), Status = "intake", History = new List<DogEvent> { new DogEvent { When = today, Event = "Intake", By = c.U.Id } } };
                    site.Dogs.Add(d);
                }
                string old = d.Status;
                d.Name = name;
                d.Breed = breed;
                d.Sex = GetDataValue("dsex") == "male" ? "male" : "female";
                d.Born = born!.Value;
                d.Size = Ui.Sizes.Any(x => x.Key == GetDataValue("dsize")) ? GetDataValue("dsize") : d.Size;
                d.Weight = weight;
                d.Color = Ui.Clip(GetDataValue("dcolor"), 40).Trim();
                d.Energy = Ui.Energies.Any(x => x.Key == GetDataValue("denergy")) ? GetDataValue("denergy") : d.Energy;
                d.Kids = Ui.GoodWith.Any(x => x.Key == GetDataValue("dkids")) ? GetDataValue("dkids") : "ask";
                d.Dogs = Ui.GoodWith.Any(x => x.Key == GetDataValue("ddogs")) ? GetDataValue("ddogs") : "ask";
                d.Cats = Ui.GoodWith.Any(x => x.Key == GetDataValue("dcats")) ? GetDataValue("dcats") : "ask";
                d.HouseTrained = GetDataValue("dhouse") == "1";
                d.Vaccinated = GetDataValue("dvacc") == "1";
                d.Fixed = GetDataValue("dfixed") == "1";
                d.Chipped = GetDataValue("dchip") == "1";
                d.Fee = fee;
                d.Needs = Ui.Clip(GetDataValue("dneeds"), 160).Trim();
                d.Intake = intake!.Value;
                d.Story = Ui.Clip(GetDataValue("dstory"), 3000).Replace("\r", string.Empty).Trim();
                d.Traits = Ui.SplitLines(GetDataValue("dtraits"), 6, 40);
                d.HappyTail = Ui.Clip(GetDataValue("dtail"), 800).Trim();
                d.Photos = photos;
                d.Health = health.OrderByDescending(h => h.When).ToList();
                string oldPair = d.Bonded;
                string pair = site.Dogs.Any(x => x.Id == GetDataValue("dbonded") && x.Id != d.Id) ? GetDataValue("dbonded") : string.Empty;
                d.Bonded = pair;
                if (oldPair != pair)
                {
                    foreach (Pet o in site.Dogs.Where(x => x.Id == oldPair && x.Bonded == d.Id))
                    {
                        o.Bonded = string.Empty;
                    }
                    foreach (Pet o in site.Dogs.Where(x => x.Id == pair))
                    {
                        o.Bonded = d.Id;
                    }
                }
                if (canX)
                {
                    d.Featured = GetDataValue("dfeat") == "1";
                    d.Urgent = GetDataValue("durgent") == "1";
                    if (want != string.Empty && want != old)
                    {
                        d.Status = want;
                        d.History.Add(new DogEvent { When = today, Event = Ui.NameOf(Ui.DogStatuses, want), By = c.U.Id });
                        if (want is "available" or "hold" or "foster" && d.Listed == null)
                        {
                            d.Listed = today;
                        }
                    }
                    if (d.Status == "adopted")
                    {
                        d.Adopter = Ui.Clip(GetDataValue("dadopter"), 60).Trim();
                        d.Adopted = Ui.ParseDay(GetDataValue("dadopted")) ?? today;
                        d.Featured = false;
                        d.Urgent = false;
                    }
                }
                saved = d.Id;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, isNew ? "add dog" : "dog", saved + " · " + name, true);
            response.ExecuteScript("AdminDogsJs.saved('" + Ui.Js(saved) + "'," + (isNew ? "true" : "false") + ");");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDogsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Refuse(response, c, "delete dog", GetDataValue("did"), "Deleting needs the Publish right.");
            }
            string id = Ui.Clip(GetDataValue("did"), 20);
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Pet? d = site.Dogs.FirstOrDefault(x => x.Id == id);
                if (d == null)
                {
                    return response;
                }
                name = d.Name;
                site.Dogs.Remove(d);
                foreach (Pet o in site.Dogs.Where(x => x.Bonded == id))
                {
                    o.Bonded = string.Empty;
                }
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete dog", id + " · " + name, true);
            response.ExecuteScript("AdminDogsJs.deleted();");
            return response;
        }

    }
}
