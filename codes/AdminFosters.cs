using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminFosters : WebPage
    {
        private const string AreaKey = "fosters";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Fosters");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--fz-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("fz-menu", Auth.Menu("fz", "AdminFosters", c, site));
            response.SetElementContents("fz-who", Auth.Who("fz", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminFostersJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"fz-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"fz-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminFostersJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("fz-main", NoAccess("Fosters"));
                return response;
            }
            response.SetElementContents("fz-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminFostersJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"fz-tr fz-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string NotesHtml(List<Note> notes, Dictionary<string, Staff> staff, bool canE, string hint)
        {
            StringBuilder sb = new StringBuilder("<h3>Notes</h3><div class=\"fz-notes\">");
            if (notes.Count == 0)
            {
                sb.Append("<p class=\"fz-hint\">No notes yet.</p>");
            }
            foreach (Note n in notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"fz-note\">" + Ui.Enc(n.Text) + "<small>" + Ui.Enc(Ui.StaffName(staff, n.By)) + " &middot; " + Ui.Time(n.When) + "</small></div>");
            }
            sb.Append("</div>");
            if (canE)
            {
                sb.Append("<div class=\"fz-form\" style=\"margin-top:10px\"><label class=\"fz-field\"><span>Add a note</span><textarea id=\"fz-enote\" rows=\"2\" maxlength=\"500\" placeholder=\"" + Ui.Attr(hint) + "\"></textarea></label><div><button type=\"button\" class=\"fz-btn fz-btn-sm\" onclick=\"AdminFostersJs.note()\">Save note</button></div></div>");
            }
            return sb.ToString();
        }

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"fz-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"fz-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private const int Size = 20;

        private static string Views(string v)
        {
            return "<div class=\"fz-views\" role=\"tablist\"><button type=\"button\" class=\"fz-chip" + (v == "homes" ? " fz-act" : string.Empty) + "\" onclick=\"AdminFostersJs.view('homes')\" aria-pressed=\"" + (v == "homes" ? "true" : "false") + "\">Foster homes</button><button type=\"button\" class=\"fz-chip" + (v == "stays" ? " fz-act" : string.Empty) + "\" onclick=\"AdminFostersJs.view('stays')\" aria-pressed=\"" + (v == "stays" ? "true" : "false") + "\">Dogs in foster</button></div>";
        }

        private static string Bars(SiteData site, string v, string st)
        {
            if (v == "stays")
            {
                return "<div class=\"fz-bar\"><label>Search<input type=\"search\" id=\"fz-q\" list=\"fz-qlist\" placeholder=\"Dog or foster home\" maxlength=\"40\" oninput=\"AdminFostersJs.search()\"></label>" + Ui.DataList("fz-qlist", site.Placements.Select(p => Ui.DogName(site, p.Dog)).Concat(site.Fosters.Select(f => f.Name))) + "<label>Show<select id=\"fz-sst\" onchange=\"AdminFostersJs.list(1)\">" + Ui.Opts(new[] { ("now", "In foster now"), ("late", "Past the planned date"), ("ended", "Ended stays"), ("all", "Every stay") }, "now") + "</select></label></div><div class=\"fz-tbl fz-sttbl\" id=\"fz-list\"></div><div id=\"fz-pager\"></div>";
            }
            return "<div class=\"fz-bar\"><label>Search<input type=\"search\" id=\"fz-q\" list=\"fz-qlist\" placeholder=\"Name, email or phone\" maxlength=\"40\" oninput=\"AdminFostersJs.search()\"></label>" + Ui.DataList("fz-qlist", site.Fosters.Select(f => f.Name)) + "<label>Status<select id=\"fz-fst\" onchange=\"AdminFostersJs.list(1)\"><option value=\"\">All but declined</option>" + Ui.Opts(Ui.FosterStatuses.Select(s => (s.Key, s.Name)), Ui.FosterStatuses.Any(s => s.Key == st) ? st : string.Empty) + "<option value=\"all\">Everything</option></select></label><label>Takes<select id=\"fz-fsize\" onchange=\"AdminFostersJs.list(1)\"><option value=\"\">Any size</option>" + Ui.Opts(Ui.Sizes.Select(s => (s.Key, s.Name + " dogs")), string.Empty) + "<option value=\"medical\">Medical care</option><option value=\"puppies\">Puppies</option></select></label></div><div class=\"fz-tbl fz-fhtbl\" id=\"fz-list\"></div><div id=\"fz-pager\"></div>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string[] a = (arg ?? string.Empty).Split('|');
            string v = a[0] == "stays" ? "stays" : "homes";
            int now = site.Placements.Count(p => p.Ended == null);
            int homes = site.Fosters.Count(f => f.Status == "approved");
            return "<div class=\"fz-ptop\"><div><h1>Fosters</h1><p>" + Ui.Plural(homes, "approved foster home", "approved foster homes") + " &middot; " + Ui.Plural(now, "dog", "dogs") + " in foster now.</p></div></div>" + Views(v) + "<div id=\"fz-vbox\">" + Bars(site, v, a.Length > 1 ? a[1] : string.Empty) + "</div>";
        }

        public async Task<ApiResponse> View()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            string v = GetDataValue("fview") == "stays" ? "stays" : "homes";
            response.SetElementContents("fz-vbox", Bars(site, v, string.Empty));
            response.ExecuteScript("document.querySelectorAll('.fz-views .fz-chip').forEach(function (b, i) { var on = (i === 1) === " + (v == "stays" ? "true" : "false") + "; b.classList.toggle('fz-act', on); b.setAttribute('aria-pressed', on ? 'true' : 'false'); }); try { history.replaceState(null, '', 'AdminFosters" + (v == "stays" ? "?view=stays" : string.Empty) + "'); } catch (e) {} AdminFostersJs.list(1);");
            return response;
        }

        private static string SizesText(FosterHome f)
        {
            return f.Sizes.Count == 0 || f.Sizes.Count == Ui.Sizes.Length ? "Any size" : string.Join(", ", Ui.Sizes.Where(s => f.Sizes.Contains(s.Key)).Select(s => s.Name));
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            string term = Ui.Clip(GetDataValue("fq"), 40);
            string st = GetDataValue("fst");
            string size = GetDataValue("fsize");
            IEnumerable<FosterHome> q = site.Fosters.Where(f => (st == "all" || (st == string.Empty ? f.Status != "declined" : f.Status == st)) && (size == string.Empty || (size == "medical" ? f.Medical : size == "puppies" ? f.Puppies : f.Sizes.Count == 0 || f.Sizes.Contains(size))));
            if (term != string.Empty)
            {
                q = q.Where(f => (f.Name + " " + f.Email + " " + f.Phone + " " + f.Id).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<FosterHome> all = q.OrderBy(f => f.Status == "new" ? 0 : 1).ThenByDescending(f => f.Created).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Foster home", string.Empty), ("Home", string.Empty), ("Can take", string.Empty), ("Fostering now", string.Empty), ("Status", string.Empty), ("Signed up", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"fz-tr\"><span class=\"fz-none fz-wide\">No foster homes match.</span></div>");
            }
            foreach (FosterHome f in all.Skip((page - 1) * Size).Take(Size))
            {
                List<string> dogs = site.Placements.Where(p => p.Home == f.Id && p.Ended == null).Select(p => Ui.DogName(site, p.Dog)).ToList();
                string tags = (f.Medical ? "<span class=\"fz-tag\">Medical</span>" : string.Empty) + (f.Puppies ? "<span class=\"fz-tag\">Puppies</span>" : string.Empty);
                sb.Append("<div class=\"fz-tr" + (f.Status == "new" ? " fz-fresh" : string.Empty) + "\"><span class=\"fz-wide\"><b>" + Ui.Enc(f.Name) + "</b><small>" + Ui.Enc(f.Email) + " &middot; " + Ui.Enc(f.Phone) + "</small></span><span data-l=\"Home\">" + Ui.NameOf(Ui.HomeKinds, f.Home) + "<small>" + Ui.NameOf(Ui.Yards, f.Yard) + "</small></span><span data-l=\"Can take\">" + SizesText(f) + "<small>" + Ui.NameOf(Ui.Lengths, f.Length) + "</small>" + (tags != string.Empty ? "<span class=\"fz-tags\">" + tags + "</span>" : string.Empty) + "</span><span data-l=\"Fostering now\">" + (dogs.Count > 0 ? "<b>" + Ui.Enc(string.Join(", ", dogs)) + "</b>" : "<i class=\"fz-hint\">Nobody</i>") + "</span><span data-l=\"Status\">" + Ui.Pill("fz", Ui.FosterStatuses, f.Status) + "</span><span data-l=\"Signed up\">" + Ui.Time(f.Created) + "</span><span><button type=\"button\" class=\"fz-btn fz-btn-sm fz-btn-ghost\" onclick=\"AdminFostersJs.open('" + Ui.Attr(f.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("fz-list", sb.ToString());
            response.SetElementContents("fz-pager", Ui.Pager("fz", page, Size, all.Count, "foster home", "foster homes", "AdminFostersJs.list"));
            return response;
        }

        public async Task<ApiResponse> Stays()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            string term = Ui.Clip(GetDataValue("sq"), 40);
            string st = GetDataValue("sst");
            IEnumerable<Placement> q = site.Placements.Where(p => st switch
            {
                "late" => p.Ended == null && p.Until != null && p.Until.Value < today,
                "ended" => p.Ended != null,
                "all" => true,
                _ => p.Ended == null
            });
            if (term != string.Empty)
            {
                q = q.Where(p => (Ui.DogName(site, p.Dog) + " " + (site.Fosters.FirstOrDefault(f => f.Id == p.Home)?.Name ?? string.Empty) + " " + p.Note).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<Placement> all = q.OrderBy(p => p.Ended == null ? 0 : 1).ThenByDescending(p => p.Ended ?? p.Start).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Dog", string.Empty), ("Foster home", string.Empty), ("Started", string.Empty), ("Planned until", string.Empty), ("Days", "fz-num"), ("Note", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"fz-tr\"><span class=\"fz-none fz-wide\">No stays match.</span></div>");
            }
            foreach (Placement p in all.Skip((page - 1) * Size).Take(Size))
            {
                Pet? d = Ui.DogOf(site, p.Dog);
                FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == p.Home);
                bool late = p.Ended == null && p.Until != null && p.Until.Value < today;
                int days = Math.Max(0, ((p.Ended ?? today) - p.Start.Date).Days);
                sb.Append("<div class=\"fz-tr\"><span class=\"fz-wide\"><span class=\"fz-dmini\">" + (d != null && d.Photos.Count > 0 ? "<img src=\"" + Ui.Attr(Ui.Thumb(Ui.Cover(site, d))) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Enc(d?.Name ?? p.Dog) + "</b><small>" + (d != null ? Ui.Enc(Ui.NameOf(Ui.DogStatuses, d.Status)) : "Removed") + "</small></span></span></span><span data-l=\"Foster home\">" + Ui.Enc(f?.Name ?? "Removed home") + "<small>" + Ui.Enc(f?.Phone ?? string.Empty) + "</small></span><span data-l=\"Started\">" + p.Start.ToString("d MMM yyyy", Ui.Inv) + "</span><span data-l=\"Planned until\"" + (late ? " class=\"fz-late\"" : string.Empty) + ">" + (p.Ended != null ? "Ended " + p.Ended.Value.ToString("d MMM", Ui.Inv) : p.Until != null ? p.Until.Value.ToString("d MMM yyyy", Ui.Inv) + (late ? "<small>Past due</small>" : string.Empty) : "<i class=\"fz-hint\">Open-ended</i>") + "</span><span data-l=\"Days\" class=\"fz-num\">" + days + "</span><span data-l=\"Note\" title=\"" + Ui.Attr(p.Note) + "\">" + Ui.Enc(p.Note) + "</span><span><button type=\"button\" class=\"fz-btn fz-btn-sm fz-btn-ghost\" onclick=\"AdminFostersJs.stay('" + Ui.Attr(p.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("fz-list", sb.ToString());
            response.SetElementContents("fz-pager", Ui.Pager("fz", page, Size, all.Count, "stay", "stays", "AdminFostersJs.list"));
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "foster home", GetDataValue("fid"), "You don't have access to fosters.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("fid"), 20));
            if (f == null)
            {
                response.ExecuteScript("AdminFostersJs.toast('That foster home no longer exists.');");
                return response;
            }
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            bool canE = Access.Can(c.U, AreaKey, 'E');
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(f.Name) + " <small style=\"font-family:var(--fz-text);font-size:14px;color:var(--fz-ink2)\">" + Ui.Enc(f.Id) + "</small></h2><dl class=\"fz-dl\"><dt>Status</dt><dd>" + Ui.Pill("fz", Ui.FosterStatuses, f.Status) + "</dd><dt>Signed up</dt><dd>" + Ui.Time(f.Created, "full") + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(f.Email) + "\">" + Ui.Enc(f.Email) + "</a></dd><dt>Phone</dt><dd><a href=\"tel:" + Ui.Attr(Ui.Digits(f.Phone)) + "\">" + Ui.Enc(f.Phone) + "</a></dd><dt>Address</dt><dd>" + Ui.Enc(f.Address) + "</dd><dt>Home</dt><dd>" + Ui.NameOf(Ui.HomeKinds, f.Home) + " &middot; " + Ui.NameOf(Ui.Yards, f.Yard) + "</dd><dt>Children</dt><dd>" + Ui.Enc(f.Kids != string.Empty ? f.Kids : "None") + "</dd><dt>Pets at home</dt><dd>" + Ui.Enc(f.Pets != string.Empty ? f.Pets : "None") + "</dd><dt>Can take</dt><dd>" + SizesText(f) + " &middot; " + Ui.NameOf(Ui.Lengths, f.Length) + "</dd><dt>Also</dt><dd>" + (f.Medical || f.Puppies ? string.Join(", ", new[] { f.Medical ? "Dogs with medical needs" : string.Empty, f.Puppies ? "Puppies" : string.Empty }.Where(s => s != string.Empty)) : "No medical cases or puppies") + "</dd></dl>");
            if (f.Message != string.Empty)
            {
                sb.Append("<h3>Message</h3><div class=\"fz-lmsg\">" + Ui.Enc(f.Message) + "</div>");
            }
            if (canE)
            {
                sb.Append("<h3>Status</h3><div class=\"fz-stbtns\">");
                foreach ((string k, string n) in Ui.FosterStatuses)
                {
                    sb.Append("<button type=\"button\" class=\"fz-chip" + (k == f.Status ? " fz-act" : string.Empty) + "\" onclick=\"AdminFostersJs.status('" + k + "')\"" + (k == f.Status ? " aria-pressed=\"true\"" : string.Empty) + ">" + n + "</button>");
                }
                sb.Append("</div>");
            }
            List<Placement> stays = site.Placements.Where(p => p.Home == f.Id).OrderBy(p => p.Ended == null ? 0 : 1).ThenByDescending(p => p.Start).ToList();
            sb.Append("<h3>Stays</h3>");
            if (stays.Count == 0)
            {
                sb.Append("<p class=\"fz-hint\">No dogs placed here yet.</p>");
            }
            else
            {
                sb.Append("<div class=\"fz-stays\">");
                foreach (Placement p in stays)
                {
                    sb.Append("<div><span><b>" + Ui.Enc(Ui.DogName(site, p.Dog)) + "</b> &middot; " + p.Start.ToString("MMM d", Ui.Inv) + " to " + (p.Ended != null ? p.Ended.Value.ToString("MMM d, yyyy", Ui.Inv) : "now") + "</span><span class=\"fz-hint\">" + (p.Ended == null ? "In foster" : Ui.Plural(Math.Max(0, (p.Ended.Value - p.Start.Date).Days), "day", "days")) + "</span></div>");
                }
                sb.Append("</div>");
            }
            if (canE && f.Status == "approved")
            {
                List<Pet> free = site.Dogs.Where(d => d.Status != "adopted" && !site.Placements.Any(p => p.Dog == d.Id && p.Ended == null)).OrderBy(d => d.Name).ToList();
                sb.Append("<h3>Place a dog here</h3>");
                if (free.Count == 0)
                {
                    sb.Append("<p class=\"fz-hint\">Every dog in our care is already in foster.</p>");
                }
                else
                {
                    sb.Append("<div class=\"fz-form\"><div class=\"fz-place\"><label class=\"fz-field\"><span>Dog</span><select id=\"fz-pdog\">" + Ui.Opts(free.Select(d => (d.Id, d.Name + " (" + Ui.NameOf(Ui.DogStatuses, d.Status) + ")")), string.Empty) + "</select></label>" + F("pstart", "Starts", Ui.Ymd(today), 10, "date") + F("puntil", "Planned until (optional)", string.Empty, 10, "date") + "</div>" + F("pnote", "Note", string.Empty, 160, "text", " placeholder=\"Recovering from surgery, needs quiet\"") + "<em class=\"fz-err\" id=\"fz-e-place\" role=\"alert\"></em><div><button type=\"button\" class=\"fz-btn fz-btn-sm\" onclick=\"AdminFostersJs.place()\">Place dog</button></div></div>");
                }
            }
            else if (canE)
            {
                sb.Append("<p class=\"fz-hint\" style=\"margin-top:10px\">Approve this home to place dogs here.</p>");
            }
            sb.Append(NotesHtml(f.Notes, staff, canE, "Home check done. Calm house, two cats."));
            sb.Append("<div class=\"fz-mfoot\">" + (canE && !stays.Any(p => p.Ended == null) ? "<button type=\"button\" class=\"fz-btn fz-btn-danger fz-left\" onclick=\"AdminFostersJs.remove('" + Ui.Js(f.Name) + "')\">Delete</button>" : string.Empty) + "<button type=\"button\" class=\"fz-btn fz-btn-ghost\" onclick=\"AdminFostersJs.closeModal()\">Close</button></div>");
            response.SetElementContents("fz-mbody", sb.ToString());
            response.ExecuteScript("AdminFostersJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SetStatus()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "foster status", GetDataValue("fid"), "You can look but not change foster homes.");
            }
            string to = Ui.FosterStatuses.Any(s => s.Key == GetDataValue("fst2")) ? GetDataValue("fst2") : string.Empty;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("fid"), 20));
                if (f == null || to == string.Empty)
                {
                    return response;
                }
                f.Status = to;
                name = f.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "foster " + to, name, true);
            response.ExecuteScript("AdminFostersJs.changed('Marked " + Ui.Js(Ui.NameOf(Ui.FosterStatuses, to).ToLowerInvariant()) + ".');");
            return response;
        }

        public async Task<ApiResponse> Place()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "place dog", GetDataValue("fid"), "You can look but not change foster homes.");
            }
            DateTime? start = Ui.ParseDay(GetDataValue("pstart"));
            DateTime? until = Ui.ParseDay(GetDataValue("puntil"));
            string err = string.Empty;
            string msg = string.Empty;
            string target = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("fid"), 20));
                Pet? d = Ui.DogOf(site, Ui.Clip(GetDataValue("pdog"), 20));
                if (f == null || f.Status != "approved")
                {
                    err = "Approve this foster home first.";
                }
                else if (d == null || d.Status == "adopted")
                {
                    err = "Choose a dog that is in our care.";
                }
                else if (site.Placements.Any(p => p.Dog == d.Id && p.Ended == null))
                {
                    err = d.Name + " is already in foster.";
                }
                else if (start == null || start.Value > today.AddDays(60) || start.Value < today.AddDays(-60))
                {
                    err = "Check the start date.";
                }
                else if (until != null && until.Value <= start.Value)
                {
                    err = "The planned end must be after the start.";
                }
                if (err == string.Empty)
                {
                    site.Placements.Add(new Placement { Id = Store.NextId(site.Placements.Select(p => p.Id), "P-", 100), Dog = d!.Id, Home = f!.Id, Start = start!.Value, Until = until, Note = Ui.Clip(GetDataValue("pnote"), 160).Trim() });
                    if (d.Status == "available")
                    {
                        d.Status = "foster";
                    }
                    d.History.Add(new DogEvent { When = start.Value, Event = "Went to foster with " + f.Name, By = c.U.Id });
                    target = d.Name + " → " + f.Name;
                    msg = d.Name + " placed with " + f.Name + ".";
                    await Store.SaveSite(site);
                }
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("document.getElementById('fz-e-place').textContent = '" + Ui.Js(err) + "';");
                return response;
            }
            await Store.Log(c.U.Id, "place dog", target, true);
            response.ExecuteScript("AdminFostersJs.changed('" + Ui.Js(msg) + "');");
            return response;
        }

        public async Task<ApiResponse> Stay()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "foster stay", GetDataValue("sid"), "You don't have access to fosters.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            Placement? p = site.Placements.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("sid"), 20));
            if (p == null)
            {
                response.ExecuteScript("AdminFostersJs.toast('That stay no longer exists.');");
                return response;
            }
            bool canE = Access.Can(c.U, AreaKey, 'E');
            Pet? d = Ui.DogOf(site, p.Dog);
            FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == p.Home);
            string dn = d?.Name ?? p.Dog;
            string fn = f?.Name ?? "the foster home";
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(dn) + " with " + Ui.Enc(fn) + "</h2><dl class=\"fz-dl\"><dt>Dog</dt><dd>" + (d != null ? "<a href=\"AdminDogs?id=" + Uri.EscapeDataString(d.Id) + "\">" + Ui.Enc(d.Name) + "</a> &middot; " + Ui.Pill("fz", Ui.DogStatuses, d.Status) : Ui.Enc(p.Dog)) + "</dd><dt>Foster home</dt><dd>" + (f != null ? "<a href=\"#\" onclick=\"AdminFostersJs.open('" + Ui.Attr(f.Id) + "');return false\">" + Ui.Enc(f.Name) + "</a> &middot; <a href=\"tel:" + Ui.Attr(Ui.Digits(f.Phone)) + "\">" + Ui.Enc(f.Phone) + "</a>" : "Removed") + "</dd><dt>Started</dt><dd>" + p.Start.ToString("ddd, MMM d, yyyy", Ui.Inv) + "</dd>" + (p.Ended != null ? "<dt>Ended</dt><dd>" + p.Ended.Value.ToString("ddd, MMM d, yyyy", Ui.Inv) + "</dd>" : string.Empty) + "<dt>Length</dt><dd>" + Ui.Plural(Math.Max(0, ((p.Ended ?? today) - p.Start.Date).Days), "day", "days") + "</dd></dl>");
            if (canE && p.Ended == null)
            {
                sb.Append("<h3>Plan</h3><div class=\"fz-form\"><div class=\"fz-f2\">" + F("suntil", "Planned until", p.Until != null ? Ui.Ymd(p.Until.Value) : string.Empty, 10, "date") + F("snote", "Note", p.Note, 160) + "</div><em class=\"fz-err\" id=\"fz-e-stay\" role=\"alert\"></em><div><button type=\"button\" class=\"fz-btn fz-btn-sm\" onclick=\"AdminFostersJs.saveStay()\">Save plan</button></div></div>");
                sb.Append("<h3>End the stay</h3><div class=\"fz-inl\"><label class=\"fz-field\"><span>Ended on</span><input type=\"date\" id=\"fz-send\" value=\"" + Ui.Ymd(today) + "\"></label><button type=\"button\" class=\"fz-btn fz-btn-sm fz-btn-ghost\" onclick=\"AdminFostersJs.endStay('" + Ui.Js(dn) + "','" + Ui.Js(fn) + "')\">End stay</button></div>");
            }
            else if (p.Note != string.Empty)
            {
                sb.Append("<h3>Note</h3><div class=\"fz-lmsg\">" + Ui.Enc(p.Note) + "</div>");
            }
            sb.Append("<div class=\"fz-mfoot\"><button type=\"button\" class=\"fz-btn fz-btn-ghost\" onclick=\"AdminFostersJs.closeModal()\">Close</button></div>");
            response.SetElementContents("fz-mbody", sb.ToString());
            response.ExecuteScript("AdminFostersJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SaveStay()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "foster plan", GetDataValue("sid"), "You can look but not change foster stays.");
            }
            DateTime? until = Ui.ParseDay(GetDataValue("suntil"));
            string err = string.Empty;
            string target = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Placement? p = site.Placements.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("sid"), 20));
                if (p == null || p.Ended != null)
                {
                    return response;
                }
                if (until != null && until.Value <= p.Start)
                {
                    err = "The planned end must be after the start.";
                }
                else
                {
                    p.Until = until;
                    p.Note = Ui.Clip(GetDataValue("snote"), 160).Trim();
                    target = Ui.DogName(site, p.Dog);
                    await Store.SaveSite(site);
                }
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("document.getElementById('fz-e-stay').textContent = '" + Ui.Js(err) + "';");
                return response;
            }
            await Store.Log(c.U.Id, "foster plan", target, true);
            response.ExecuteScript("AdminFostersJs.stayDone('Plan saved.');");
            return response;
        }

        public async Task<ApiResponse> EndStay()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "end foster stay", GetDataValue("sid"), "You can look but not change foster stays.");
            }
            DateTime? end = Ui.ParseDay(GetDataValue("send"));
            string msg = string.Empty;
            string target = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                Placement? p = site.Placements.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("sid"), 20));
                if (p == null || p.Ended != null)
                {
                    return response;
                }
                DateTime when = end == null || end.Value < p.Start || end.Value > today ? today : end.Value;
                p.Ended = when;
                Pet? d = Ui.DogOf(site, p.Dog);
                FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == p.Home);
                msg = "Stay ended.";
                if (d != null)
                {
                    d.History.Add(new DogEvent { When = when, Event = "Back from foster" + (f != null ? " with " + f.Name : string.Empty), By = c.U.Id });
                    if (d.Status == "foster")
                    {
                        d.Status = "available";
                        msg = "Stay ended. " + d.Name + " is Available again.";
                    }
                }
                target = Ui.DogName(site, p.Dog);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "end foster stay", target, true);
            response.ExecuteScript("AdminFostersJs.stayDone('" + Ui.Js(msg) + "');");
            return response;
        }

        public async Task<ApiResponse> AddNote()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "foster note", GetDataValue("fid"), "You can look but not change foster homes.");
            }
            string text = Regex.Replace(Ui.Clip(GetDataValue("enote"), 500), "\\s+", " ").Trim();
            if (text.Length < 2)
            {
                response.ExecuteScript("AdminFostersJs.toast('Write the note first.');");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("fid"), 20));
                if (f == null)
                {
                    return response;
                }
                f.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = text });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "foster note", GetDataValue("fid"), true);
            response.ExecuteScript("AdminFostersJs.changed('Note saved.');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminFostersJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "delete foster home", GetDataValue("fid"), "You can look but not change foster homes.");
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                FosterHome? f = site.Fosters.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("fid"), 20));
                if (f == null)
                {
                    return response;
                }
                if (site.Placements.Any(p => p.Home == f.Id && p.Ended == null))
                {
                    return await Refuse(response, c, "delete foster home", f.Name, "End the current stays first.");
                }
                name = f.Name;
                site.Fosters.Remove(f);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete foster home", name, true);
            response.ExecuteScript("AdminFostersJs.deleted();");
            return response;
        }
    }
}
