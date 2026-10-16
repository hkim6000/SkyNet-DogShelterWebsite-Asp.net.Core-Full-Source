using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminDonations : WebPage
    {
        private const string AreaKey = "donations";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Donations");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--dm-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("dm-menu", Auth.Menu("dm", "AdminDonations", c, site));
            response.SetElementContents("dm-who", Auth.Who("dm", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminDonationsJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"dm-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"dm-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminDonationsJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("dm-main", NoAccess("Donations"));
                return response;
            }
            response.SetElementContents("dm-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminDonationsJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"dm-tr dm-th\">");
            foreach ((string n, string cls) in cols)
            {
                sb.Append("<span" + (cls != string.Empty ? " class=\"" + cls + "\"" : string.Empty) + ">" + n + "</span>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string NotesHtml(List<Note> notes, Dictionary<string, Staff> staff, bool canE, string hint)
        {
            StringBuilder sb = new StringBuilder("<h3>Notes</h3><div class=\"dm-notes\">");
            if (notes.Count == 0)
            {
                sb.Append("<p class=\"dm-hint\">No notes yet.</p>");
            }
            foreach (Note n in notes.OrderByDescending(n => n.When))
            {
                sb.Append("<div class=\"dm-note\">" + Ui.Enc(n.Text) + "<small>" + Ui.Enc(Ui.StaffName(staff, n.By)) + " &middot; " + Ui.Time(n.When) + "</small></div>");
            }
            sb.Append("</div>");
            if (canE)
            {
                sb.Append("<div class=\"dm-form\" style=\"margin-top:10px\"><label class=\"dm-field\"><span>Add a note</span><textarea id=\"dm-enote\" rows=\"2\" maxlength=\"500\" placeholder=\"" + Ui.Attr(hint) + "\"></textarea></label><div><button type=\"button\" class=\"dm-btn dm-btn-sm\" onclick=\"AdminDonationsJs.note()\">Save note</button></div></div>");
            }
            return sb.ToString();
        }

        private static string F(string id, string label, string value, int max, string type = "text", string extra = "")
        {
            return "<label class=\"dm-field\"><span>" + label + "</span><input type=\"" + type + "\" id=\"dm-" + id + "\" maxlength=\"" + max + "\" value=\"" + Ui.Attr(value) + "\"" + extra + "></label>";
        }

        private const int Size = 20;

        private static string Amount(Donation d)
        {
            return Ui.Money(d.Amount) + (d.Every == "monthly" ? "<small>a month</small>" : string.Empty);
        }

        private static string FundText(SiteData site, Donation d)
        {
            return d.Fund == "sponsor" && d.Dog != string.Empty ? "Sponsor " + Ui.Enc(Ui.DogName(site, d.Dog)) : Ui.NameOf(Ui.Funds, d.Fund);
        }

        private static string Summary(SiteData site)
        {
            DateTime today = Store.Today(site);
            DateTime month = new DateTime(today.Year, today.Month, 1);
            List<Donation> rec = site.Donations.Where(d => d.Status == "received" && d.Received != null).ToList();
            decimal m = rec.Where(d => d.Received!.Value >= month && d.Received.Value <= today).Sum(d => d.Amount);
            decimal y = rec.Where(d => d.Received!.Value.Year == today.Year && d.Received.Value <= today).Sum(d => d.Amount);
            List<Donation> open = site.Donations.Where(d => d.Status == "pledged").ToList();
            int monthly = site.Donations.Where(d => d.Every == "monthly" && d.Status != "cancelled").Select(d => d.Email.ToLowerInvariant()).Distinct().Count();
            return "<div><b>" + Ui.Whole(m) + "</b><span>Received in " + today.ToString("MMMM", Ui.Inv) + "</span></div><div><b>" + Ui.Whole(y) + "</b><span>Received in " + today.Year + "</span></div><div><b>" + Ui.Whole(open.Sum(d => d.Amount)) + "</b><span>" + Ui.Plural(open.Count, "open pledge", "open pledges") + "</span></div><div><b>" + monthly + "</b><span>Monthly donors</span></div>";
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            string st = Ui.DonStatuses.Any(s => s.Key == arg) ? arg : string.Empty;
            bool canE = Access.Can(c.U, AreaKey, 'E');
            bool canX = Access.Can(c.U, AreaKey, 'X');
            StringBuilder sb = new StringBuilder("<div class=\"dm-ptop\"><div><h1>Donations</h1><p>Pledges from the website and gifts recorded by staff. Mark a pledge received when the check, cash or transfer arrives.</p></div><div class=\"dm-acts\">" + (canX ? "<button type=\"button\" class=\"dm-btn dm-btn-ghost\" onclick=\"AdminDonationsJs.exportCsv()\">Export CSV</button>" : string.Empty) + (canE ? "<button type=\"button\" class=\"dm-btn dm-btn-accent\" onclick=\"AdminDonationsJs.add()\">Record a gift</button>" : string.Empty) + "</div></div><div class=\"dm-vsum\" id=\"dm-vsum\">" + Summary(site) + "</div>");
            sb.Append("<div class=\"dm-bar\"><label>Search<input type=\"search\" id=\"dm-q\" list=\"dm-qlist\" placeholder=\"Name, email, ID or dedication\" maxlength=\"60\" oninput=\"AdminDonationsJs.search()\"></label>" + Ui.DataList("dm-qlist", site.Donations.Select(d => d.Name)));
            sb.Append("<label>Status<select id=\"dm-dst\" onchange=\"AdminDonationsJs.list(1)\"><option value=\"\">Every status</option>" + Ui.Opts(Ui.DonStatuses.Select(s => (s.Key, s.Name)), st) + "</select></label>");
            sb.Append("<label>Fund<select id=\"dm-dfund\" onchange=\"AdminDonationsJs.list(1)\"><option value=\"\">Every fund</option>" + Ui.Opts(Ui.Funds.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>Method<select id=\"dm-dmeth\" onchange=\"AdminDonationsJs.list(1)\"><option value=\"\">Any method</option>" + Ui.Opts(Ui.Methods.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label>");
            sb.Append("<label>How often<select id=\"dm-devery\" onchange=\"AdminDonationsJs.list(1)\"><option value=\"\">One time and monthly</option>" + Ui.Opts(Ui.Everys.Select(s => (s.Key, s.Name)), string.Empty) + "</select></label></div>");
            sb.Append("<div class=\"dm-bar\" id=\"dm-perbar\">" + Ui.PeriodBar("dm", "AdminDonationsJs.per", "all", string.Empty, string.Empty) + "</div><div class=\"dm-tbl dm-dmtbl\" id=\"dm-list\"></div><div class=\"dm-tot\" id=\"dm-tot\"></div><div id=\"dm-pager\"></div>");
            return sb.ToString();
        }

        private List<Donation> Filtered(SiteData site, out string per)
        {
            string term = Ui.Clip(GetDataValue("dq"), 60);
            string st = GetDataValue("dst");
            string fund = GetDataValue("dfund");
            string meth = GetDataValue("dmeth");
            string every = GetDataValue("devery");
            per = Per(GetDataValue("per"));
            (DateTime? from, DateTime? to) = Ui.Period(Store.Today(site), per, GetDataValue("pfrom"), GetDataValue("pto"));
            IEnumerable<Donation> q = site.Donations.Where(d => (st == string.Empty || d.Status == st) && (fund == string.Empty || d.Fund == fund) && (meth == string.Empty || d.Method == meth) && (every == string.Empty || d.Every == every) && Ui.InPeriod(Store.Local(site, d.Created), from, to));
            if (term != string.Empty)
            {
                q = q.Where(d => (d.Name + " " + d.Email + " " + d.Id + " " + d.Honor + " " + Ui.DogName(site, d.Dog)).Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            return q.OrderByDescending(d => d.Created).ToList();
        }

        public async Task<ApiResponse> List()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            List<Donation> all = Filtered(site, out string per);
            int page = Ui.PageOf(GetDataValue("pg"), Size, all.Count);
            StringBuilder sb = new StringBuilder(Head(new[] { ("Donor", string.Empty), ("Amount", "dm-num"), ("Fund", string.Empty), ("Method", string.Empty), ("Pledged", string.Empty), ("Status", string.Empty), (string.Empty, string.Empty) }));
            if (all.Count == 0)
            {
                sb.Append("<div class=\"dm-tr\"><span class=\"dm-none dm-wide\">No gifts match.</span></div>");
            }
            foreach (Donation d in all.Skip((page - 1) * Size).Take(Size))
            {
                sb.Append("<div class=\"dm-tr\"><span class=\"dm-wide\"><b>" + Ui.Enc(d.Name) + "</b>" + (d.Anonymous ? " <span class=\"dm-tag\">Private</span>" : string.Empty) + "<small>" + Ui.Enc(d.Email) + (d.Honor != string.Empty ? " &middot; " + Ui.Enc(d.Honor) : string.Empty) + "</small></span><span data-l=\"Amount\" class=\"dm-num dm-amt\">" + Amount(d) + "</span><span data-l=\"Fund\">" + FundText(site, d) + "</span><span data-l=\"Method\">" + Ui.NameOf(Ui.Methods, d.Method) + "</span><span data-l=\"Pledged\">" + Ui.Time(d.Created) + "</span><span data-l=\"Status\">" + Ui.Pill("dm", Ui.DonStatuses, d.Status) + (d.Received != null && d.Status == "received" ? "<small>" + d.Received.Value.ToString("d MMM", Ui.Inv) + "</small>" : string.Empty) + "</span><span><button type=\"button\" class=\"dm-btn dm-btn-sm dm-btn-ghost\" onclick=\"AdminDonationsJs.open('" + Ui.Attr(d.Id) + "')\">Open</button></span></div>");
            }
            decimal rec = all.Where(d => d.Status == "received").Sum(d => d.Amount);
            decimal pl = all.Where(d => d.Status == "pledged").Sum(d => d.Amount);
            response.SetElementContents("dm-list", sb.ToString());
            response.SetElementContents("dm-tot", all.Count > 0 ? "<span>Received <b>" + Ui.Money(rec) + "</b></span><span>Pledged <b>" + Ui.Money(pl) + "</b></span>" : string.Empty);
            response.SetElementContents("dm-pager", Ui.Pager("dm", page, Size, all.Count, "gift", "gifts", "AdminDonationsJs.list"));
            response.SetElementContents("dm-perbar", Ui.PeriodBar("dm", "AdminDonationsJs.per", per, GetDataValue("pfrom"), GetDataValue("pto")));
            response.SetElementContents("dm-vsum", Summary(site));
            return response;
        }

        public async Task<ApiResponse> Export()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Refuse(response, c, "export donations", "csv", "Exporting needs the Export right.");
            }
            SiteData site = await Store.LoadSite();
            List<Donation> all = Filtered(site, out string per);
            StringBuilder sb = new StringBuilder("ID,Pledged,Name,Email,Phone,Amount,How often,Fund,Dog,Method,Status,Received,Private,Dedication,Message\r\n");
            foreach (Donation d in all)
            {
                sb.Append(string.Join(",", new[] { d.Id, Store.Local(site, d.Created).ToString("yyyy-MM-dd HH:mm", Ui.Inv), d.Name, d.Email, d.Phone, d.Amount.ToString("0.00", Ui.Inv), Ui.NameOf(Ui.Everys, d.Every), Ui.NameOf(Ui.Funds, d.Fund), d.Dog != string.Empty ? Ui.DogName(site, d.Dog) : string.Empty, Ui.NameOf(Ui.Methods, d.Method), Ui.NameOf(Ui.DonStatuses, d.Status), d.Received != null ? Ui.Ymd(d.Received.Value) : string.Empty, d.Anonymous ? "Yes" : "No", d.Honor, d.Message }.Select(Ui.Csv)) + "\r\n");
            }
            await Store.Log(c.U.Id, "export donations", Ui.Plural(all.Count, "row", "rows"), true);
            response.ExecuteScript("AdminDonationsJs.download('donations-" + Store.Today(site).ToString("yyyy-MM-dd", Ui.Inv) + ".csv'," + JsonSerializer.Serialize(sb.ToString()) + ");");
            return response;
        }

        public async Task<ApiResponse> Open()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "donation", GetDataValue("did"), "You don't have access to donations.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            Donation? d = site.Donations.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("did"), 20));
            if (d == null)
            {
                response.ExecuteScript("AdminDonationsJs.toast('That gift no longer exists.');");
                return response;
            }
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            bool canE = Access.Can(c.U, AreaKey, 'E');
            bool canX = Access.Can(c.U, AreaKey, 'X');
            Pet? dog = Ui.DogOf(site, d.Dog);
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Money(d.Amount) + (d.Every == "monthly" ? " a month" : string.Empty) + " from " + Ui.Enc(d.Name) + " <small style=\"font-family:var(--dm-text);font-size:14px;color:var(--dm-ink2)\">" + Ui.Enc(d.Id) + "</small></h2><dl class=\"dm-dl\"><dt>Status</dt><dd>" + Ui.Pill("dm", Ui.DonStatuses, d.Status) + (d.Received != null && d.Status == "received" ? " on " + d.Received.Value.ToString("MMM d, yyyy", Ui.Inv) : string.Empty) + "</dd><dt>Pledged</dt><dd>" + Ui.Time(d.Created, "full") + "</dd><dt>Fund</dt><dd>" + Ui.NameOf(Ui.Funds, d.Fund) + (dog != null ? " &middot; <a href=\"AdminDogs?id=" + Uri.EscapeDataString(dog.Id) + "\">" + Ui.Enc(dog.Name) + "</a>" : string.Empty) + "</dd><dt>How often</dt><dd>" + Ui.NameOf(Ui.Everys, d.Every) + "</dd><dt>Paying by</dt><dd>" + Ui.NameOf(Ui.Methods, d.Method) + "</dd><dt>Email</dt><dd><a href=\"mailto:" + Ui.Attr(d.Email) + "\">" + Ui.Enc(d.Email) + "</a></dd>" + (d.Phone != string.Empty ? "<dt>Phone</dt><dd><a href=\"tel:" + Ui.Attr(Ui.Digits(d.Phone)) + "\">" + Ui.Enc(d.Phone) + "</a></dd>" : string.Empty) + (d.Honor != string.Empty ? "<dt>Dedication</dt><dd>" + Ui.Enc(d.Honor) + "</dd>" : string.Empty) + "<dt>Thank-you lists</dt><dd>" + (d.Anonymous ? "Keep name private" : "Name can be listed") + "</dd></dl>");
            if (d.Message != string.Empty)
            {
                sb.Append("<h3>Message</h3><div class=\"dm-lmsg\">" + Ui.Enc(d.Message) + "</div>");
            }
            if (canE)
            {
                string who = Ui.Js(d.Name);
                sb.Append("<h3>Payment</h3>");
                if (d.Status == "pledged")
                {
                    sb.Append("<div class=\"dm-dec\"><label class=\"dm-field\"><span>Received on</span><input type=\"date\" id=\"dm-dday\" value=\"" + Ui.Ymd(today) + "\"></label><button type=\"button\" class=\"dm-btn dm-btn-sm dm-ok\" style=\"min-height:44px\" onclick=\"AdminDonationsJs.receive()\">Mark received</button><button type=\"button\" class=\"dm-btn dm-btn-sm dm-btn-danger\" style=\"min-height:44px\" onclick=\"AdminDonationsJs.status('cancelled','" + who + "')\">Cancel pledge</button></div>");
                }
                else
                {
                    sb.Append("<div class=\"dm-dec\"><button type=\"button\" class=\"dm-btn dm-btn-sm dm-btn-ghost\" onclick=\"AdminDonationsJs.status('pledged','" + who + "')\">Set back to pledged</button></div>");
                }
            }
            sb.Append(NotesHtml(d.Notes, staff, canE, "Thank-you card sent."));
            sb.Append("<div class=\"dm-mfoot\">" + (canX ? "<button type=\"button\" class=\"dm-btn dm-btn-danger dm-left\" onclick=\"AdminDonationsJs.remove('" + Ui.Js(d.Name) + "')\">Delete</button>" : string.Empty) + "<button type=\"button\" class=\"dm-btn dm-btn-ghost\" onclick=\"AdminDonationsJs.closeModal()\">Close</button></div>");
            response.SetElementContents("dm-mbody", sb.ToString());
            response.ExecuteScript("AdminDonationsJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> SetStatus()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "donation status", GetDataValue("did"), "You can look but not change donations.");
            }
            string to = Ui.DonStatuses.Any(s => s.Key == GetDataValue("dst2")) ? GetDataValue("dst2") : string.Empty;
            DateTime? day = Ui.ParseDay(GetDataValue("dday"));
            string target = string.Empty;
            string msg = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                Donation? d = site.Donations.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("did"), 20));
                if (d == null || to == string.Empty || to == d.Status)
                {
                    return response;
                }
                if (to == "received")
                {
                    DateTime when = day == null || day.Value > today || day.Value < today.AddYears(-1) ? today : day.Value;
                    d.Received = when;
                    d.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Received " + when.ToString("MMM d", Ui.Inv) + "." });
                    msg = "Marked received.";
                }
                else if (to == "cancelled")
                {
                    d.Received = null;
                    d.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Pledge cancelled." });
                    msg = "Pledge cancelled.";
                }
                else
                {
                    d.Received = null;
                    d.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Set back to pledged." });
                    msg = "Set back to pledged.";
                }
                d.Status = to;
                target = d.Id + " · " + d.Name + " · " + d.Amount.ToString("0.##", Ui.Inv);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "donation " + to, target, true);
            response.ExecuteScript("AdminDonationsJs.changed('" + Ui.Js(msg) + "');");
            return response;
        }

        public async Task<ApiResponse> AddForm()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "record gift", "new", "You can look but not record gifts.");
            }
            SiteData site = await Store.LoadSite();
            DateTime today = Store.Today(site);
            StringBuilder sb = new StringBuilder("<h2>Record a gift</h2><p class=\"dm-hint\">For gifts that come in by mail, at the shelter or at events. Website pledges appear here on their own.</p><div class=\"dm-form\" style=\"margin-top:12px\"><div class=\"dm-f2\">" + F("nname", "Donor name", string.Empty, 80) + F("nmail", "Email", string.Empty, 120, "email") + "</div>");
            sb.Append("<div class=\"dm-f4\">" + F("namt", "Amount ($)", string.Empty, 9, "text", " inputmode=\"decimal\" placeholder=\"50\"") + "<label class=\"dm-field\"><span>How often</span><select id=\"dm-nevery\">" + Ui.Opts(Ui.Everys.Select(x => (x.Key, x.Name)), "once") + "</select></label><label class=\"dm-field\"><span>Paid by</span><select id=\"dm-nmeth\">" + Ui.Opts(Ui.Methods.Select(x => (x.Key, x.Name)), "check") + "</select></label>" + F("nday", "Received on", Ui.Ymd(today), 10, "date") + "</div>");
            sb.Append("<div class=\"dm-f2\"><label class=\"dm-field\"><span>Fund</span><select id=\"dm-nfund\" onchange=\"AdminDonationsJs.fundView()\">" + Ui.Opts(Ui.Funds.Select(x => (x.Key, x.Name)), "general") + "</select></label><label class=\"dm-field\" id=\"dm-ndogw\" hidden><span>Dog</span><select id=\"dm-ndog\">" + Ui.Opts(site.Dogs.Where(x => x.Status != "adopted").OrderBy(x => x.Name).Select(x => (x.Id, x.Name)), string.Empty) + "</select></label></div>");
            sb.Append("<div class=\"dm-f2\">" + F("nphone", "Phone (optional)", string.Empty, 30, "tel") + F("nhonor", "In honor or memory of (optional)", string.Empty, 80) + "</div>");
            sb.Append("<label class=\"dm-field\"><span>Note (optional)</span><textarea id=\"dm-nmsg\" rows=\"2\" maxlength=\"500\"></textarea></label><div class=\"dm-checks\"><label class=\"dm-chk\"><input type=\"checkbox\" id=\"dm-nanon\"> Keep name private in thank-you lists</label></div><em class=\"dm-err\" id=\"dm-e-new\" role=\"alert\"></em></div>");
            sb.Append("<div class=\"dm-mfoot\"><button type=\"button\" class=\"dm-btn dm-btn-ghost\" onclick=\"AdminDonationsJs.closeModal()\">Cancel</button><button type=\"button\" class=\"dm-btn dm-btn-accent\" onclick=\"AdminDonationsJs.saveNew()\">Record gift</button></div>");
            response.SetElementContents("dm-mbody", sb.ToString());
            response.ExecuteScript("AdminDonationsJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> AddGift()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "record gift", "new", "You can look but not record gifts.");
            }
            string name = Regex.Replace(Ui.Clip(GetDataValue("nname"), 80), "\\s+", " ").Trim();
            string mail = Ui.Clip(GetDataValue("nmail"), 120).Trim();
            decimal amount = decimal.TryParse(Regex.Replace(GetDataValue("namt"), "[$,\\s]", string.Empty), NumberStyles.Number, Ui.Inv, out decimal av) ? Math.Round(av, 2) : 0;
            string fund = Ui.Funds.Any(x => x.Key == GetDataValue("nfund")) ? GetDataValue("nfund") : "general";
            DateTime? day = Ui.ParseDay(GetDataValue("nday"));
            string err = string.Empty;
            if (name.Length < 2)
            {
                err = "Enter the donor's name.";
            }
            else if (mail != string.Empty && !Ui.Email(mail))
            {
                err = "Check the email address, or leave it empty.";
            }
            else if (amount < 1 || amount > 1000000)
            {
                err = "Enter an amount from $1 to $1,000,000.";
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminDonationsJs.failNew('" + Ui.Js(err) + "');");
                return response;
            }
            string id = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                DateTime today = Store.Today(site);
                Pet? dog = fund == "sponsor" ? Ui.DogOf(site, Ui.Clip(GetDataValue("ndog"), 20)) : null;
                if (fund == "sponsor" && dog == null)
                {
                    err = "Choose the dog this gift sponsors.";
                }
                else
                {
                    DateTime when = day == null || day.Value > today || day.Value < today.AddYears(-1) ? today : day.Value;
                    id = Store.NextId(site.Donations.Select(x => x.Id), "DN-", 5000);
                    Donation d = new Donation { Id = id, Name = name, Email = mail, Phone = Ui.CleanPhone(GetDataValue("nphone")), Amount = amount, Every = GetDataValue("nevery") == "monthly" ? "monthly" : "once", Fund = fund, Dog = dog?.Id ?? string.Empty, Honor = Ui.Clip(GetDataValue("nhonor"), 80).Trim(), Method = Ui.Methods.Any(x => x.Key == GetDataValue("nmeth")) ? GetDataValue("nmeth") : "check", Anonymous = GetDataValue("nanon") == "1", Message = string.Empty, Status = "received", Created = DateTime.UtcNow, Received = when };
                    string note = Regex.Replace(Ui.Clip(GetDataValue("nmsg"), 500), "\\s+", " ").Trim();
                    d.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = "Recorded by staff." + (note != string.Empty ? " " + note : string.Empty) });
                    site.Donations.Insert(0, d);
                    await Store.SaveSite(site);
                }
            }
            if (err != string.Empty)
            {
                response.ExecuteScript("AdminDonationsJs.failNew('" + Ui.Js(err) + "');");
                return response;
            }
            await Store.Log(c.U.Id, "record gift", id + " · " + name + " · " + amount.ToString("0.##", Ui.Inv), true);
            response.ExecuteScript("AdminDonationsJs.added('" + Ui.Js(id) + "');");
            return response;
        }

        public async Task<ApiResponse> AddNote()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "donation note", GetDataValue("did"), "You can look but not change donations.");
            }
            string text = Regex.Replace(Ui.Clip(GetDataValue("enote"), 500), "\\s+", " ").Trim();
            if (text.Length < 2)
            {
                response.ExecuteScript("AdminDonationsJs.toast('Write the note first.');");
                return response;
            }
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Donation? d = site.Donations.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("did"), 20));
                if (d == null)
                {
                    return response;
                }
                d.Notes.Add(new Note { When = DateTime.UtcNow, By = c.U.Id, Text = text });
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "donation note", GetDataValue("did"), true);
            response.ExecuteScript("AdminDonationsJs.changed('Note saved.');");
            return response;
        }

        public async Task<ApiResponse> Delete()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminDonationsJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'X'))
            {
                return await Refuse(response, c, "delete donation", GetDataValue("did"), "Deleting gifts needs the Export right.");
            }
            string target = string.Empty;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                Donation? d = site.Donations.FirstOrDefault(x => x.Id == Ui.Clip(GetDataValue("did"), 20));
                if (d == null)
                {
                    return response;
                }
                target = d.Id + " · " + d.Name + " · " + d.Amount.ToString("0.##", Ui.Inv);
                site.Donations.Remove(d);
                await Store.SaveSite(site);
            }
            await Store.Log(c.U.Id, "delete donation", target, true);
            response.ExecuteScript("AdminDonationsJs.deleted();");
            return response;
        }
    }
}
