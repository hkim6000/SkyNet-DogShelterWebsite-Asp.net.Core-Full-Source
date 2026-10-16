using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminApplications : WebPage
    {
        private const string AreaKey = "applications";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Applications");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--ap-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("ap-menu", Auth.Menu("ap", "AdminApplications", c, site));
            response.SetElementContents("ap-who", Auth.Who("ap", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminApplicationsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"ap-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"ap-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminApplicationsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("ap-main", NoAccess("Applications"));
                return response;
            }
            response.SetElementContents("ap-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminApplicationsJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"ap-tr ap-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string NotesHtml(List<Note> notes, Dictionary<string, Staff> staff, bool canE, string hint)
        {
            StringBuilder sb = new StringBuilder("<h3>Notes</h3><div class=\"ap-notes\">");
            if (notes.Count == 0)
            {
                sb.Append("<p class=\"ap-hint\">No notes yet.</p>");
            }
            foreach (Note n in notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"ap-note\">" + Ui.Enc(n.Text) + "<small>" + Ui.Enc(Ui.StaffName(staff, n.By)) + " &middot; " + Ui.Time(n.When) + "</small></div>");
            }
            sb.Append("</div>");
            if (canE)
            {
                sb.Append("<div class=\"ap-form\" style=\"margin-top:10px\"><label class=\"ap-field\"><span>Add a note</span><textarea id=\"ap-enote\" rows=\"2\" maxlength=\"500\" placeholder=\"" + Ui.Attr(hint) + "\"></textarea></label><div><button type=\"button\" class=\"ap-btn ap-btn-sm\" onclick=\"AdminApplicationsJs.note()\">Save note</button></div></div>");
            }
            return sb.ToString();
        }

        private const int Size = 20;

        private static readonly string[] Closed = { "adopted", "declined", "withdrawn" };

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            List<Staff> staff = await Store.LoadStaff();
            string st = Ui.AppStatuses.Any(s => s.Key == arg) ? arg : string.Empty;
            StringBuilder sb = new StringBuilder("<div class=\"ap-ptop\"><div><h1>Applications</h1><p>Adoption applications from the website. Review, visit, then approve, decline or mark the dog adopted.</p></div></div>");
            sb.Append("<div class=\"ap-bar\"><label>Search<input type=\"search\" id=\"ap-q\" list=\"ap-qlist\" placeholder=\"Name, email, phone or dog\" maxlength=\"60\" oninput=\"AdminApplicationsJs.search()\"></label>" + Ui.DataList("ap-qlist", site.Applications.SelectMany(x => new[] { x.Name, x.Email, Ui.DogName(site, x.Dog) })));
            sb.Append("<label>Status<select id=\"ap-ast\" onchange=\"AdminApplicationsJs.list(1)\"><option value=\"\">Open (in progress)</option>" + Ui.Opts(Ui.AppStatuses.Select(s => (s.Key, s.Name)), st) + "<option value=\"all\">Everything</option></select></label>");
            sb.Append("<label>Dog<select id=\"ap-adog\" onchange=\"AdminApplicationsJs.list(1)\"><option value=\"\">Any dog</option><option value=\"none\">No dog chosen</option>" + Ui.Opts(site.Dogs.Where(d => site.Applications.Any(a => a.Dog == d.Id)).OrderBy(d => d.Name).Select(d => (d.Id, d.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>Reviewer<select id=\"ap-arev\" onchange=\"AdminApplicationsJs.list(1)\"><option value=\"\">Everyone</option><option value=\"none\">Nobody yet</option>" + Ui.Opts(staff.Where(u => u.Active && Access.Can(u, "applications", 'E')).OrderBy(u => u.Name).Select(u => (u.Id, u.Name)), string.Empty) + "</select></label></div>");
            sb.Append("<div class=\"ap-bar\" id=\"ap-perbar\">" + Ui.PeriodBar("ap", "AdminApplicationsJs.per", "all", string.Empty, string.Empty) + "</div><div class=\"ap-tbl ap-aptbl\" id=\"ap-list\"></div><div id=\"ap-pager\"></div>");
            return sb.ToString();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            string term = Ui.Clip(GetDataValue("aq"), 60);
            string st = GetDataValue("ast");
            string dog = GetDataValue("adog");
            string rev = GetDataValue("arev");
            string per = Per(GetDataValue("per"));
            (DateTime? from, DateTime? to) = Ui.Period(Store.Today(site), per, GetDataValue("pfrom"), GetDataValue("pto"));
            IEnumerable<Application> q = site.Applications.Where(x => (st == "all" || (st == string.Empty ? !Closed.Contains(x.Status) : x.Status == st)) && (dog == string.Empty || (dog == "none" ? x.Dog == string.Empty : x.Dog == dog)) && (rev == string.Empty || (rev == "none" ? x.Reviewer == string.Empty : x.Reviewer == rev)) && Ui.InPeriod(Store.Local(site, x.Created), from, to));
            if (term != string.Empty)
            {
                q = q.Where(x => (x.Name + " " + x.Email + " " + x.Phone + " " + Ui.DogName(site, x.Dog) + " " + x.Id).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            List<Application> all = q.OrderByDescending(x => x.Created).ToList();
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Person", string.Empty), ("Dog", string.Empty), ("Home", string.Empty), ("Received", string.Empty), ("Reviewer", string.Empty), ("Status", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"ap-tr\"><span class=\"ap-none ap-wide\">No applications match.</span></div>");
            }
            foreach (Application x in all.Skip((page - 1) * Size).Take(Size))
            {
                Pet? d = Ui.DogOf(site, x.Dog);
                sb.Append("<div class=\"ap-tr" + (x.Status == "new" ? " ap-fresh" : string.Empty) + "\"><span class=\"ap-wide\"><b>" + Ui.Enc(x.Name) + "</b><small>" + Ui.Enc(x.Email) + " &middot; " + Ui.Enc(x.Phone) + "</small></span><span data-l=\"Dog\">" + (d != null ? "<span class=\"ap-dmini\">" + (d.Photos.Count > 0 ? "<img src=\"" + Ui.Attr(Ui.Thumb(Ui.Cover(site, d))) + "\" alt=\"\" loading=\"lazy\">" : string.Empty) + "<span><b>" + Ui.Enc(d.Name) + "</b><small>" + Ui.Enc(Ui.NameOf(Ui.DogStatuses, d.Status)) + "</small></span></span>" : "<i class=\"ap-hint\">Help me choose</i>") + "</span><span data-l=\"Home\">" + Ui.NameOf(Ui.HomeKinds, x.Home) + "<small>" + (x.Owns == "rent" ? "Rents" : "Owns") + " &middot; " + Ui.NameOf(Ui.Yards, x.Yard) + "</small></span><span data-l=\"Received\">" + Ui.Time(x.Created) + "</span><span data-l=\"Reviewer\">" + (x.Reviewer != string.Empty ? Ui.Enc(Ui.StaffName(staff, x.Reviewer)) : "<i class=\"ap-hint\">Nobody yet</i>") + "</span><span data-l=\"Status\">" + Ui.Pill("ap", Ui.AppStatuses, x.Status) + "</span><span><button type=\"button\" class=\"ap-btn ap-btn-sm ap-btn-ghost\" onclick=\"AdminApplicationsJs.open('" + Ui.Attr(x.Id) + "')\">Open</button></span></div>");
            }
            response.SetElementContents("ap-list", sb.ToString());
            response.SetElementContents("ap-pager", Ui.Pager("ap", page, Size, all.Count, "application", "applications", "AdminApplicationsJs.list"));
            response.SetElementContents("ap-perbar", Ui.PeriodBar("ap", "AdminApplicationsJs.per", per, GetDataValue("pfrom"), GetDataValue("pto")));
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "application", GetDataValue("aid"), "You don't have access to applications.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
            if (x == null)
            {
                response.ExecuteScript("AdminApplicationsJs.toast('That application no longer exists.');");
                return response;
            }
            List<Staff> list = await Store.LoadStaff();
            Dictionary<string, Staff> staff = Ui.StaffMap(list);
            bool canE = Access.Can(c.U, AreaKey, 'E');
            bool canX = Access.Can(c.U, AreaKey, 'X');
            Pet? d = Ui.DogOf(site, x.Dog);
            bool open = !Closed.Contains(x.Status);
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(x.Name) + " <small style=\"font-family:var(--ap-text);font-size:14px;color:var(--ap-ink2)\">" + Ui.Enc(x.Id) + "</small></h2><dl class=\"ap-dl\"><dt>Status</dt><dd>" + Ui.Pill("ap", Ui.AppStatuses, x.Status) + "</dd><dt>Received</dt><dd>" + Ui.Time(x.Created, "full") + "</dd><dt>Dog</dt><dd>" + (d != null ? "<a href=\"AdminDogs?id=" + Uri.EscapeDataString(d.Id) + "\"><b>" + Ui.Enc(d.Name) + "</b></a> &middot; " + Ui.Enc(d.Breed) + " &middot; " + Ui.Pill("ap", Ui.DogStatuses, d.Status) : "Not chosen yet. Suggest a dog that fits.") + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(x.Email) + "\">" + Ui.Enc(x.Email) + "</a></dd><dt>Phone</dt><dd><a href=\"tel:" + Ui.Attr(Ui.Digits(x.Phone)) + "\">" + Ui.Enc(x.Phone) + "</a></dd><dt>Address</dt><dd>" + Ui.Enc(x.Address) + "</dd><dt>Home</dt><dd>" + Ui.NameOf(Ui.HomeKinds, x.Home) + ", " + (x.Owns == "rent" ? "rents (landlord allows dogs)" : "owns") + " &middot; " + Ui.NameOf(Ui.Yards, x.Yard) + "</dd><dt>Children</dt><dd>" + Ui.Enc(x.Kids != string.Empty ? x.Kids : "Not given") + "</dd><dt>Other pets</dt><dd>" + Ui.Enc(x.Pets != string.Empty ? x.Pets : "Not given") + "</dd><dt>Alone each day</dt><dd>" + Ui.Enc(x.Alone != string.Empty ? x.Alone : "Not given") + "</dd>" + (x.Visit != null ? "<dt>Home visit</dt><dd><b>" + x.Visit.Value.ToString("ddd, MMM d", Ui.Inv) + " at " + Ui.Hm(site, x.Visit.Value) + "</b></dd>" : string.Empty) + "</dl>");
            if (x.Experience != string.Empty)
            {
                sb.Append("<h3>Experience</h3><div class=\"ap-lmsg\">" + Ui.Enc(x.Experience) + "</div>");
            }
            if (x.Message != string.Empty)
            {
                sb.Append("<h3>Message</h3><div class=\"ap-lmsg\">" + Ui.Enc(x.Message) + "</div>");
            }
            if (canE && open)
            {
                sb.Append("<h3>Progress</h3><div class=\"ap-stbtns\">");
                foreach (string k in new[] { "new", "review", "visit", "withdrawn" })
                {
                    sb.Append("<button type=\"button\" class=\"ap-chip" + (k == x.Status ? " ap-act" : string.Empty) + "\" onclick=\"AdminApplicationsJs.status('" + k + "')\"" + (k == x.Status ? " aria-pressed=\"true\"" : string.Empty) + ">" + Ui.NameOf(Ui.AppStatuses, k) + "</button>");
                }
                sb.Append("</div><div class=\"ap-inl\" style=\"margin-top:12px\"><label class=\"ap-field\"><span>Home visit</span><input type=\"datetime-local\" id=\"ap-avisit\" value=\"" + (x.Visit != null ? Ui.Stamp(x.Visit.Value) : string.Empty) + "\"></label><button type=\"button\" class=\"ap-btn ap-btn-sm ap-btn-ghost\" onclick=\"AdminApplicationsJs.visit()\">Save visit</button></div>");
                sb.Append("<h3>Reviewer</h3><div class=\"ap-inl\"><select id=\"ap-arto\" aria-label=\"Reviewer\"><option value=\"\">Nobody yet</option>" + Ui.Opts(list.Where(u => u.Active && Access.Can(u, "applications", 'E')).OrderBy(u => u.Name).Select(u => (u.Id, u.Name)), x.Reviewer) + "</select><button type=\"button\" class=\"ap-btn ap-btn-sm\" onclick=\"AdminApplicationsJs.reviewer()\">Assign</button></div>");
            }
            if (canX && open)
            {
                string who = Ui.Js(x.Name.Split(' ')[0]);
                string dn = Ui.Js(d?.Name ?? string.Empty);
                sb.Append("<h3>Decision</h3><div class=\"ap-dec\">" + (x.Status != "approved" ? "<button type=\"button\" class=\"ap-btn ap-btn-sm ap-ok\" onclick=\"AdminApplicationsJs.decide('approved','" + who + "','" + dn + "')\">Approve</button>" : string.Empty) + (d != null && d.Status != "adopted" ? "<button type=\"button\" class=\"ap-btn ap-btn-sm ap-btn-accent\" onclick=\"AdminApplicationsJs.decide('adopted','" + who + "','" + dn + "')\">Mark adopted</button>" : string.Empty) + "<button type=\"button\" class=\"ap-btn ap-btn-sm ap-btn-danger\" onclick=\"AdminApplicationsJs.decide('declined','" + who + "','" + dn + "')\">Decline</button></div>" + (d == null ? "<p class=\"ap-hint\" style=\"margin-top:6px\">Choose a dog with the family before marking an adoption: ask them to apply for that dog, or note it here.</p>" : string.Empty));
            }
            else if (!canX && open && canE)
            {
                sb.Append("<p class=\"ap-hint\" style=\"margin-top:12px\">Approving, declining and marking adopted need the Decide right.</p>");
            }
            sb.Append(NotesHtml(x.Notes, staff, canE, "Called, very keen. Booked a meet and greet for Saturday."));
            sb.Append("<div class=\"ap-mfoot\">" + (canX ? "<button type=\"button\" class=\"ap-btn ap-btn-danger ap-left\" onclick=\"AdminApplicationsJs.remove('" + Ui.Js(x.Name) + "')\">Delete</button>" : string.Empty) + "<button type=\"button\" class=\"ap-btn ap-btn-ghost\" onclick=\"AdminApplicationsJs.closeModal()\">Close</button></div>");
            response.SetElementContents("ap-mbody", sb.ToString());
            response.ExecuteScript("AdminApplicationsJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SetStatus()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "application status", GetDataValue("aid"), "You can look but not change applications.");
            }
            string to = GetDataValue("ast2") is "new" or "review" or "visit" or "withdrawn" ? GetDataValue("ast2") : string.Empty;
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
                if (x == null || to == string.Empty || Closed.Contains(x.Status))
                {
                    return await Refuse(response, c, "application status", GetDataValue("aid"), "That application is closed.");
                }
                x.Status = to;
                if (to != "new" && x.Reviewer == string.Empty)
                {
                    x.Reviewer = c.U.Id;
                }
                if (to == "withdrawn")
                {
                    x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Marked withdrawn." });
                    Release(site, x, c.U.Id);
                }
                name = x.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "application " + to, name, true);
            response.ExecuteScript("AdminApplicationsJs.changed('Marked " + Ui.Js(Ui.NameOf(Ui.AppStatuses, to).ToLowerInvariant()) + ".');");
            return response;
        }

        private static void Release(SiteData site, Application x, string by)
        {
            Pet? d = Ui.DogOf(site, x.Dog);
            if (d != null && d.Status == "hold" && !site.Applications.Any(a => a.Id != x.Id && a.Dog == d.Id && a.Status == "approved"))
            {
                bool fostered = site.Placements.Any(p => p.Dog == d.Id && p.Ended == null);
                d.Status = fostered ? "foster" : "available";
                d.History.Add(new DogEvent { When = Store.Today(site), Event = fostered ? "In foster" : "Available", By = by });
            }
        }

        public async Task<ApiResponse> Decide()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Refuse(response, c, "application decision", GetDataValue("aid"), "Approving and declining need the Decide right.");
            }
            string to = GetDataValue("ast2") is "approved" or "declined" or "adopted" ? GetDataValue("ast2") : string.Empty;
            string name = string.Empty;
            string msg = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
                if (x == null || to == string.Empty || Closed.Contains(x.Status))
                {
                    return await Refuse(response, c, "application decision", GetDataValue("aid"), "That application is closed.");
                }
                Pet? d = Ui.DogOf(site, x.Dog);
                if (to == "adopted" && (d == null || d.Status == "adopted"))
                {
                    return await Refuse(response, c, "application decision", x.Id, "Choose a dog that is still in our care first.");
                }
                x.Status = to;
                if (x.Reviewer == string.Empty)
                {
                    x.Reviewer = c.U.Id;
                }
                name = x.Name;
                if (to == "approved")
                {
                    x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Approved." });
                    if (d != null && d.Status is "available" or "foster")
                    {
                        d.Status = "hold";
                        d.History.Add(new DogEvent { When = today, Event = "Adoption pending", By = c.U.Id });
                        msg = "Approved. " + d.Name + " is now Adoption pending.";
                    }
                    else
                    {
                        msg = "Approved.";
                    }
                }
                else if (to == "declined")
                {
                    x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Declined." });
                    Release(site, x, c.U.Id);
                    msg = "Declined.";
                }
                else if (d != null)
                {
                    d.Status = "adopted";
                    d.Adopted = today;
                    d.Adopter = x.Name;
                    d.Featured = false;
                    d.Urgent = false;
                    d.History.Add(new DogEvent { When = today, Event = "Adopted", By = c.U.Id });
                    foreach (Placement p in site.Placements.Where(p => p.Dog == d.Id && p.Ended == null))
                    {
                        p.Ended = today;
                    }
                    x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Adoption complete. " + d.Name + " went home." });
                    foreach (Application o in site.Applications.Where(a => a.Id != x.Id && a.Dog == d.Id && !Closed.Contains(a.Status)))
                    {
                        o.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = d.Name + " was adopted by another family. Suggest other dogs." });
                    }
                    msg = d.Name + " is adopted. Congratulations!";
                }
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "application " + to, name, true);
            response.ExecuteScript("AdminApplicationsJs.changed('" + Ui.Js(msg) + "');");
            return response;
        }

        public async Task<ApiResponse> Assign()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "assign application", GetDataValue("aid"), "You can look but not change applications.");
            }
            string to = Ui.Clip(GetDataValue("arto"), 40);
            List<Staff> list = await Store.LoadStaff();
            Staff? u = list.FirstOrDefault(s => s.Id == to && s.Active && Access.Can(s, "applications", 'E'));
            if (to != string.Empty && u == null)
            {
                return response;
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
                if (x == null)
                {
                    return response;
                }
                x.Reviewer = to;
                x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Reviewer: " + (u?.Name ?? "nobody") + "." });
                name = x.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "assign application", name + " → " + (u?.Name ?? "nobody"), true);
            response.ExecuteScript("AdminApplicationsJs.changed('Reviewer saved.');");
            return response;
        }

        public async Task<ApiResponse> Visit()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "home visit", GetDataValue("aid"), "You can look but not change applications.");
            }
            DateTime? when = Ui.ParseStart(GetDataValue("avisit"));
            string name = string.Empty;
            string text = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
                if (x == null || Closed.Contains(x.Status))
                {
                    return response;
                }
                x.Visit = when;
                if (when != null)
                {
                    x.Status = "visit";
                    text = "Home visit booked for " + when.Value.ToString("ddd, MMM d", Ui.Inv) + " at " + Ui.Hm(site, when.Value) + ".";
                    x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = text });
                }
                else
                {
                    text = "Home visit cleared.";
                }
                if (x.Reviewer == string.Empty)
                {
                    x.Reviewer = c.U.Id;
                }
                name = x.Name;
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "home visit", name, true);
            response.ExecuteScript("AdminApplicationsJs.changed('" + Ui.Js(text) + "');");
            return response;
        }

        public async Task<ApiResponse> AddNote()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "application note", GetDataValue("aid"), "You can look but not change applications.");
            }
            string text = Regex.Replace(Ui.Clip(GetDataValue("enote"), 500), "\\s+", " ").Trim();
            if (text.Length < 2)
            {
                response.ExecuteScript("AdminApplicationsJs.toast('Write the note first.');");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
                if (x == null)
                {
                    return response;
                }
                x.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = text });
                if (x.Status == "new")
                {
                    x.Status = "review";
                }
                if (x.Reviewer == string.Empty)
                {
                    x.Reviewer = c.U.Id;
                }
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "application note", GetDataValue("aid"), true);
            response.ExecuteScript("AdminApplicationsJs.changed('Note saved.');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminApplicationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Refuse(response, c, "delete application", GetDataValue("aid"), "Deleting applications needs the Decide right.");
            }
            string name = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Application? x = site.Applications.FirstOrDefault(a => a.Id == Ui.Clip(GetDataValue("aid"), 20));
                if (x == null)
                {
                    return response;
                }
                name = x.Name;
                if (!Closed.Contains(x.Status))
                {
                    Release(site, x, c.U.Id);
                }
                site.Applications.Remove(x);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete application", name, true);
            response.ExecuteScript("AdminApplicationsJs.deleted();");
            return response;
        }
    }
}
