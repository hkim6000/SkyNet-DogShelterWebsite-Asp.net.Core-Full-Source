using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DogShelter.Core;
using DogShelter.Models;
using SkyNet;

namespace DogShelter.codes
{
    public class AdminPages : WebPage
    {
        private const string AreaKey = "pages";

        public override async Task OnInitialized()
        {
            SiteData site = await Store.LoadSite();
            HtmlDoc.SetTitle(site.Settings.Name + " admin | Pages");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("robots", "noindex, nofollow");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_style}", Ui.SafeColor(site.Settings.Accent) ? "--pg-accent:" + site.Settings.Accent : string.Empty).Replace("{plhd_name}", Ui.Enc(site.Settings.Name));
        }

        private static void Chrome(ApiResponse response, Ctx c, SiteData site)
        {
            response.SetElementContents("pg-menu", Auth.Menu("pg", "AdminPages", c, site));
            response.SetElementContents("pg-who", Auth.Who("pg", c));
        }

        private static async Task<ApiResponse> Refuse(ApiResponse response, Ctx c, string action, string target, string message)
        {
            await Store.Log(c.U.Id, action, target, false);
            response.ExecuteScript("AdminPagesJs.toast('" + Ui.Js(message) + "');");
            return response;
        }

        private static string NoAccess(string title)
        {
            return "<div class=\"pg-ptop\"><div><h1>" + Ui.Enc(title) + "</h1></div></div><div class=\"pg-panel\"><p>You don't have access to this area. Ask the shelter manager to give you access in Users.</p></div>";
        }

        public async Task<ApiResponse> SignOut()
        {
            ApiResponse response = new ApiResponse();
            await Auth.Close(GetDataValue("tok"));
            response.ExecuteScript("AdminPagesJs.out();");
            return response;
        }

        public async Task<ApiResponse> Load()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
                return response;
            }
            SiteData site = await Store.LoadSite();
            Chrome(response, c, site);
            if (AreaKey != string.Empty && !Access.Can(c.U, AreaKey, 'V'))
            {
                await Store.Log(c.U.Id, "view", AreaKey, false);
                response.SetElementContents("pg-main", NoAccess("Pages"));
                return response;
            }
            response.SetElementContents("pg-main", await MainHtml(c, site, GetDataValue("arg")));
            response.ExecuteScript("AdminPagesJs.loaded();");
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
            StringBuilder sb = new StringBuilder("<div class=\"pg-tr pg-th\">");
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
            return "<button type=\"button\" class=\"pg-pki\" data-src=\"" + Ui.Attr(m.Full) + "\" data-thumb=\"" + Ui.Attr(m.Thumb) + "\" data-kind=\"" + m.Kind + "\" data-title=\"" + Ui.Attr(m.Title) + "\" onclick=\"AdminPagesJs.pkToggle(this)\" title=\"" + Ui.Attr(m.Title + (m.Tags.Count > 0 ? " · " + string.Join(", ", m.Tags) : string.Empty)) + "\"><img src=\"" + Ui.Attr(m.Thumb) + "\" alt=\"\" loading=\"lazy\">" + (vid ? "<b class=\"pg-vbadge\">" + (m.Seconds / 60) + ":" + (m.Seconds % 60).ToString("00", Ui.Inv) + "</b>" : string.Empty) + "<span>" + Ui.Enc(m.Title) + "</span><i>&#10003;</i></button>";
        }

        public async Task<ApiResponse> MediaPick()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
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
                sb.Append("<p class=\"pg-none\" style=\"grid-column:1/-1\">Nothing matches. Upload a photo, or add videos in Media.</p>");
            }
            string tags = page == 1 ? JsonSerializer.Serialize(site.Media.SelectMany(m => m.Tags).Distinct().OrderBy(t => t).ToList()) : "null";
            response.ExecuteScript("AdminPagesJs.pkShown(" + JsonSerializer.Serialize(sb.ToString()) + "," + (all.Count > page * size ? "true" : "false") + "," + tags + "," + (page > 1 ? "true" : "false") + ");");
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
                response.ExecuteScript("AdminPagesJs.expired();");
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
                response.ExecuteScript("AdminPagesJs.pkUploaded(false,'That file is not a JPG, PNG or WebP photo, or it is too large.');");
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
            response.ExecuteScript("AdminPagesJs.pkUploaded(true,'');");
            return response;
        }

        private static string CE(string f, string value, string ph, string cls, bool ro, bool multi = false)
        {
            return "<div class=\"" + cls + (multi ? " pg-ml" : string.Empty) + "\" data-f=\"" + f + "\" contenteditable=\"" + (ro ? "false" : "plaintext-only") + "\" data-ph=\"" + Ui.Attr(ph) + "\"" + (multi ? " data-ml=\"1\"" : string.Empty) + ">" + Ui.Enc(value) + "</div>";
        }

        private static string CI(string f, string value, string ph, bool ro, bool multi)
        {
            return "<div data-i=\"" + f + "\" contenteditable=\"" + (ro ? "false" : "plaintext-only") + "\" data-ph=\"" + Ui.Attr(ph) + "\"" + (multi ? " data-ml=\"1\"" : string.Empty) + ">" + Ui.Enc(value) + "</div>";
        }

        private static string RichEd(string html, string ph, bool ro)
        {
            return "<div class=\"pg-er\" data-f=\"text\" contenteditable=\"" + (ro ? "false" : "true") + "\" data-ph=\"" + Ui.Attr(ph) + "\">" + html + "</div>";
        }

        private static string MediaEd(SiteData site, string src, string allow, string ph, bool ro)
        {
            MediaItem? m = site.Media.FirstOrDefault(x => x.Full == src);
            string thumb = src == string.Empty ? string.Empty : m?.Thumb ?? src;
            bool vid = Ui.IsVideo(src);
            return "<div class=\"pg-em\" data-f=\"media\" data-src=\"" + Ui.Attr(src) + "\" data-allow=\"" + allow + "\" data-ph=\"" + Ui.Attr(ph) + "\"><button type=\"button\" class=\"pg-emb\"" + (ro ? " disabled" : " onclick=\"AdminPagesJs.media(this)\"") + " aria-label=\"" + Ui.Attr(ph) + "\">" + (thumb != string.Empty ? "<img src=\"" + Ui.Attr(thumb) + "\" alt=\"\">" + (vid ? "<span class=\"pg-emv\">Video</span>" : string.Empty) + (ro ? string.Empty : "<span class=\"pg-emc\">Change</span>") : "<span>" + Ui.Enc(ph) + "</span>") + "</button>" + (ro ? string.Empty : "<button type=\"button\" class=\"pg-emx\" onclick=\"AdminPagesJs.noMedia(this)\"" + (thumb == string.Empty ? " hidden" : string.Empty) + ">Remove</button>") + "</div>";
        }

        private static string BtnEd(string n, string label, string link, string title, bool ro)
        {
            string r = ro ? " readonly" : string.Empty;
            return "<div class=\"pg-ebtn\"><span>" + title + "</span><input type=\"text\" data-f=\"label" + n + "\" maxlength=\"40\" value=\"" + Ui.Attr(label) + "\" placeholder=\"Button text\"" + r + "><input type=\"text\" data-f=\"link" + n + "\" maxlength=\"300\" value=\"" + Ui.Attr(link) + "\" placeholder=\"Link: Dogs, Adopt#apply or https://\"" + r + "></div>";
        }

        private static string ItemEd(Block b, PartSpec spec, BlockItem it, bool ro)
        {
            bool icon = spec.ItemFields.Contains("icon");
            bool link = spec.ItemFields.Contains("link");
            (string tp, string xp) = b.Type switch
            {
                "steps" => ("Step", "What happens in this step"),
                "boxes" => ("Box title", "One item per line"),
                "faq" => ("Question", "Answer"),
                "cards" => ("Card title", "Short text"),
                _ => ("$25", "What it pays for")
            };
            StringBuilder sb = new StringBuilder("<div class=\"pg-eit\">");
            if (icon)
            {
                sb.Append("<select data-i=\"icon\" aria-label=\"Icon\"" + (ro ? " disabled" : string.Empty) + "><option value=\"\">No icon</option>" + Ui.Opts(Blocks.Icons.Select(x => (x.Key, x.Name)), it.Icon) + "</select>");
            }
            sb.Append(CI("title", it.Title, tp, ro, false) + CI("text", it.Text, xp, ro, true));
            if (link)
            {
                sb.Append("<div class=\"pg-elink\"><input type=\"text\" data-i=\"label\" maxlength=\"40\" value=\"" + Ui.Attr(it.Label) + "\" placeholder=\"Link text\"" + (ro ? " readonly" : string.Empty) + "><input type=\"text\" data-i=\"link\" maxlength=\"300\" value=\"" + Ui.Attr(it.Link) + "\" placeholder=\"Link: Dogs or https://\"" + (ro ? " readonly" : string.Empty) + "></div>");
            }
            if (!ro)
            {
                sb.Append("<div class=\"pg-ictl\"><button type=\"button\" aria-label=\"Move earlier\" onclick=\"AdminPagesJs.itemMove(this,-1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" aria-label=\"Move later\" onclick=\"AdminPagesJs.itemMove(this,1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg></button><button type=\"button\" aria-label=\"Remove\" onclick=\"AdminPagesJs.itemDrop(this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/></svg></button></div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string Ed(SiteData site, Block b, bool ro)
        {
            PartSpec? spec = Blocks.Spec(b);
            if (spec == null)
            {
                return string.Empty;
            }
            bool data = b.Type == "data";
            string x = data ? "data" : b.Type;
            StringBuilder sb = new StringBuilder("<div class=\"pg-blk pg-x-" + x + (b.Hidden ? " pg-hid" : string.Empty) + "\" data-id=\"" + Ui.Attr(b.Id) + "\" data-type=\"" + b.Type + "\" data-src=\"" + Ui.Attr(b.Source) + "\" data-hidden=\"" + (b.Hidden ? "1" : "0") + "\"><header class=\"pg-bh\">");
            if (!ro)
            {
                sb.Append("<button type=\"button\" class=\"pg-grip\" aria-label=\"Drag to move\" title=\"Drag to move\" onpointerdown=\"AdminPagesJs.grab(event,this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"currentColor\" aria-hidden=\"true\"><circle cx=\"9\" cy=\"6\" r=\"1.6\"/><circle cx=\"15\" cy=\"6\" r=\"1.6\"/><circle cx=\"9\" cy=\"12\" r=\"1.6\"/><circle cx=\"15\" cy=\"12\" r=\"1.6\"/><circle cx=\"9\" cy=\"18\" r=\"1.6\"/><circle cx=\"15\" cy=\"18\" r=\"1.6\"/></svg></button>");
            }
            sb.Append("<b>" + Ui.Enc(spec.Name) + "</b>" + (data ? "<span class=\"pg-src\">From your data</span>" : string.Empty) + "<span class=\"pg-bsp\"></span>");
            if (!ro)
            {
                sb.Append("<button type=\"button\" class=\"pg-hb\" aria-pressed=\"" + (b.Hidden ? "true" : "false") + "\" onclick=\"AdminPagesJs.hide(this)\" title=\"Hide on the website without deleting\"><span>" + (b.Hidden ? "Show" : "Hide") + "</span></button><button type=\"button\" class=\"pg-hb\" aria-label=\"Move up\" title=\"Move up\" onclick=\"AdminPagesJs.move(this,-1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 15l6-6 6 6\"/></svg></button><button type=\"button\" class=\"pg-hb\" aria-label=\"Move down\" title=\"Move down\" onclick=\"AdminPagesJs.move(this,1)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg></button>" + (data ? string.Empty : "<button type=\"button\" class=\"pg-hb pg-del\" aria-label=\"Remove block\" title=\"Remove block\" onclick=\"AdminPagesJs.drop(this)\"><svg viewBox=\"0 0 24 24\" width=\"18\" height=\"18\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3\"/></svg></button>"));
            }
            sb.Append("</header><div class=\"pg-bb\">");
            if (data)
            {
                sb.Append("<div class=\"pg-dinfo\">" + Ui.Enc(spec.Desc) + (spec.Fields == string.Empty ? " Nothing to write here: move it or hide it." : string.Empty) + "</div>");
            }
            string kick = Blocks.Has(spec, "kicker") ? CE("kicker", b.Kicker, data && b.Source == "contact" ? "Note box title, like Found a stray dog?" : "Small label above the title", "pg-ek", ro) : string.Empty;
            string title = Blocks.Has(spec, "title") ? CE("title", b.Title, "Title", "pg-et", ro) : string.Empty;
            string text = Blocks.Has(spec, "text") ? RichEd(b.Text, data && b.Source == "contact" ? "Text for the note box" : "Write here", ro) : string.Empty;
            string media = Blocks.Has(spec, "media") ? MediaEd(site, b.Media, b.Type == "quote" ? "photo" : string.Empty, b.Type == "quote" ? "Add a photo" : "Add a picture or video", ro) : string.Empty;
            string btns = Blocks.Has(spec, "buttons") ? "<div class=\"pg-ebtns\">" + BtnEd(string.Empty, b.Label, b.Link, "Main button", ro) + BtnEd("2", b.Label2, b.Link2, "Second button", ro) + "</div>" : Blocks.Has(spec, "button") ? "<div class=\"pg-ebtns\">" + BtnEd(string.Empty, b.Label, b.Link, "Button", ro) + "</div>" : Blocks.Has(spec, "label") ? "<div class=\"pg-ebtns\"><div class=\"pg-ebtn\"><span>Button text</span><input type=\"text\" data-f=\"label\" maxlength=\"40\" value=\"" + Ui.Attr(b.Label) + "\" placeholder=\"Donate\"" + (ro ? " readonly" : string.Empty) + "></div></div>" : string.Empty;
            string extra = string.Empty;
            if (Blocks.Has(spec, "extra"))
            {
                (string Key, string Name)[] ex = b.Type == "hero" ? Blocks.HeroExtras : Blocks.SplitExtras;
                extra = "<label class=\"pg-eopt\">Also show <select data-f=\"extra\"" + (ro ? " disabled" : string.Empty) + ">" + Ui.Opts(ex.Select(e => (e.Key, e.Name)), b.Extra) + "</select></label>";
            }
            if (Blocks.Has(spec, "flip"))
            {
                extra += "<label class=\"pg-chk\"><input type=\"checkbox\" data-f=\"flip\"" + (b.Flip ? " checked" : string.Empty) + (ro ? " disabled" : string.Empty) + " onchange=\"this.closest('.pg-bb').querySelector('.pg-two').classList.toggle('pg-flip',this.checked)\"> Picture on the right</label>";
            }
            switch (b.Type)
            {
                case "hero":
                    sb.Append("<div class=\"pg-two\"><div style=\"display:grid;gap:10px;min-width:0\">" + kick + title + text + btns + extra + "</div>" + media + "</div>");
                    break;
                case "split":
                    sb.Append("<div class=\"pg-two" + (b.Flip ? " pg-flip" : string.Empty) + "\">" + media + "<div style=\"display:grid;gap:10px;min-width:0\">" + kick + title + text + btns + "</div></div>" + extra);
                    break;
                case "quote":
                    sb.Append(media + CE("text", b.Text, "The quote", "pg-eq", ro, true) + CE("title", b.Title, "Name, for example Maya, adopted Bear", "pg-et", ro));
                    break;
                case "media":
                    sb.Append(media + CE("text", b.Text, "Caption (optional)", "pg-hint2", ro));
                    break;
                default:
                    sb.Append(kick + title + text + (media != string.Empty ? "<div style=\"max-width:420px\">" + media + "</div>" : string.Empty) + btns + extra);
                    break;
            }
            if (Blocks.Has(spec, "items"))
            {
                sb.Append("<div><div class=\"pg-eitems\" data-f=\"items\">");
                foreach (BlockItem it in b.Items)
                {
                    sb.Append(ItemEd(b, spec, it, ro));
                }
                sb.Append("</div>" + (ro ? string.Empty : "<button type=\"button\" class=\"pg-eadd\" style=\"margin-top:10px\" onclick=\"AdminPagesJs.addItem(this)\">+ Add " + (b.Type switch { "steps" => "a step", "faq" => "a question", "boxes" => "a box", "cards" => "a card", _ => "a line" }) + "</button><template>" + ItemEd(b, spec, new BlockItem(), false) + "</template>") + "</div>");
            }
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private static string Tabs(string cur)
        {
            return string.Concat(Blocks.Pages.Select(p => "<button type=\"button\" class=\"pg-chip" + (p.Key == cur ? " pg-act" : string.Empty) + "\" aria-pressed=\"" + (p.Key == cur ? "true" : "false") + "\" onclick=\"AdminPagesJs.tab('" + p.Key + "')\">" + Ui.Enc(p.Name) + "</button>"));
        }

        private static async Task<string> MainHtml(Ctx c, SiteData site, string arg)
        {
            bool canE = Access.Can(c.U, AreaKey, 'E');
            return "<div class=\"pg-ptop\"><div><h1>Pages</h1><p>" + (canE ? "Click any text to change it. Drag a block by its handle, or use the arrows, to move it. Use {name} or {town} to show the shelter's name or town from Settings. Changes go live when you save." : "You can look at the pages but not change them.") + "</p></div></div><div class=\"pg-ptools\"><div class=\"pg-ptabs\" id=\"pg-ptabs\">" + Tabs("home") + "</div><div class=\"pg-pacts\"><span class=\"pg-dirty\" id=\"pg-dirty\"></span><button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-ghost\" onclick=\"AdminPagesJs.versions()\">Versions</button><button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-ghost\" id=\"pg-viewlink\" data-href=\"Home\" onclick=\"AdminPagesJs.view()\">View page</button>" + (canE ? "<button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-accent\" id=\"pg-savebtn\" onclick=\"AdminPagesJs.save()\">Save page</button>" : string.Empty) + "</div></div><div class=\"pg-canvas\" id=\"pg-canvas\"></div><div id=\"pg-holder\" hidden></div><div class=\"pg-rt\" id=\"pg-rt\" role=\"toolbar\" aria-label=\"Text style\"><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('bold')\" title=\"Bold\"><b>B</b></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('italic')\" title=\"Italic\"><i>I</i></button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('link')\" title=\"Link\">Link</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('insertUnorderedList')\" title=\"List\">&bull; List</button><button type=\"button\" onmousedown=\"event.preventDefault()\" onclick=\"AdminPagesJs.fmt('clear')\" title=\"Clear formatting\">Clear</button></div>";
        }

        private static string Canvas(SiteData site, PageSpec p, bool canE)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Block b in site.Pages[p.Key].Blocks)
            {
                sb.Append(Ed(site, b, !canE));
            }
            if (canE)
            {
                sb.Append("<div class=\"pg-addp\" id=\"pg-addp\"><b>Add a block</b><div class=\"pg-addl\">" + string.Concat(Blocks.Types.Select(t => "<button type=\"button\" onclick=\"AdminPagesJs.addBlock('" + t.Key + "')\"><b>" + Ui.Enc(t.Name) + "</b><small>" + Ui.Enc(t.Desc) + "</small></button>")) + "</div><p class=\"pg-hint2\" style=\"margin-top:10px\">Blocks marked From your data (dogs, events, forms) cannot be removed, but you can move them, change their titles or hide them.</p></div>");
            }
            return sb.ToString();
        }

        public async Task<ApiResponse> Show()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            PageSpec p = Blocks.PageOf(GetDataValue("page")) ?? Blocks.Pages[0];
            bool canE = Access.Can(c.U, AreaKey, 'E');
            response.SetElementContents("pg-canvas", Canvas(site, p, canE));
            response.SetElementContents("pg-ptabs", Tabs(p.Key));
            response.ExecuteScript("document.getElementById('pg-viewlink').setAttribute('data-href','" + p.Href + "');AdminPagesJs.shown('" + p.Key + "');");
            return response;
        }

        public async Task<ApiResponse> AddBlock()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "add block", GetDataValue("btype"), "You can look but not change pages.");
            }
            PartSpec? t = Blocks.Types.FirstOrDefault(x => x.Key == GetDataValue("btype"));
            if (t == null)
            {
                return response;
            }
            SiteData site = await Store.LoadSite();
            Block b = new Block { Id = Blocks.NewId(), Type = t.Key };
            if (t.Key is "cards" or "steps" or "boxes" or "faq")
            {
                b.Items.Add(new BlockItem());
                b.Items.Add(new BlockItem());
                b.Items.Add(new BlockItem());
            }
            response.SetElementContents("pg-holder", Ed(site, b, false));
            response.ExecuteScript("AdminPagesJs.added();");
            return response;
        }

        public async Task<ApiResponse> Save()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "save page", GetDataValue("page"), "You can look but not change pages.");
            }
            PageSpec? p = Blocks.PageOf(GetDataValue("page"));
            if (p == null)
            {
                return response;
            }
            string err = string.Empty;
            SiteData? done = null;
            using (await Store.Hold())
            {
                SiteData site = await Store.LoadSite();
                (List<Block>? list, string e) = Blocks.Clean(site, p, GetDataValue("blocks"));
                if (list == null)
                {
                    err = e;
                }
                else
                {
                    PageDoc doc = site.Pages[p.Key];
                    doc.Versions.Insert(0, new PageVersion { When = doc.Saved == default ? DateTime.UtcNow.AddMinutes(-1) : doc.Saved, By = doc.By, Blocks = doc.Blocks });
                    if (doc.Versions.Count > 5)
                    {
                        doc.Versions.RemoveRange(5, doc.Versions.Count - 5);
                    }
                    doc.Blocks = list;
                    doc.Saved = DateTime.UtcNow;
                    doc.By = c.U.Id;
                    await Store.SaveSite(site);
                    done = site;
                }
            }
            if (done == null)
            {
                response.ExecuteScript("AdminPagesJs.fail('" + Ui.Js(err) + "');");
                return response;
            }
            await Store.Log(c.U.Id, "save page", p.Name, true);
            response.SetElementContents("pg-canvas", Canvas(done, p, true));
            response.ExecuteScript("AdminPagesJs.shown('" + p.Key + "');AdminPagesJs.saved('" + Ui.Js(p.Name + " saved. It is live on the website.") + "');");
            return response;
        }

        public async Task<ApiResponse> Versions()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'V'))
            {
                return await Refuse(response, c, "page versions", GetDataValue("page"), "You don't have access to pages.");
            }
            SiteData site = await Store.LoadSite();
            PageSpec? p = Blocks.PageOf(GetDataValue("page"));
            if (p == null)
            {
                return response;
            }
            PageDoc doc = site.Pages[p.Key];
            Dictionary<string, Staff> staff = Ui.StaffMap(await Store.LoadStaff());
            bool canE = Access.Can(c.U, AreaKey, 'E');
            StringBuilder sb = new StringBuilder("<h2>" + Ui.Enc(p.Name) + ": versions</h2><p class=\"pg-hint\" style=\"margin:-4px 0 14px\">The last five saved versions are kept. Restoring one makes it live right away.</p><div class=\"pg-vlist\"><div class=\"pg-vrow\"><div><b>Live now</b><small>" + (doc.Saved != default ? "Saved " + Ui.Time(doc.Saved, "full") + (doc.By != string.Empty ? " by " + Ui.Enc(Ui.StaffName(staff, doc.By)) : string.Empty) : "Sample content") + " &middot; " + Ui.Plural(doc.Blocks.Count, "block", "blocks") + "</small></div></div>");
            if (doc.Versions.Count == 0)
            {
                sb.Append("<p class=\"pg-hint\">No earlier versions yet. They appear here after you save.</p>");
            }
            for (int i = 0; i < doc.Versions.Count; i++)
            {
                PageVersion v = doc.Versions[i];
                string when = Store.Local(site, v.When).ToString("MMM d, h:mm tt", Ui.Inv);
                sb.Append("<div class=\"pg-vrow\"><div><b>" + Ui.Time(v.When, "full") + "</b><small>" + (v.By != string.Empty ? Ui.Enc(Ui.StaffName(staff, v.By)) + " &middot; " : string.Empty) + Ui.Plural(v.Blocks.Count, "block", "blocks") + "</small></div>" + (canE ? "<button type=\"button\" class=\"pg-btn pg-btn-sm pg-btn-ghost\" onclick=\"AdminPagesJs.restore(" + i + ",'" + Ui.Js(when) + "')\">Restore</button>" : string.Empty) + "</div>");
            }
            sb.Append("</div><div class=\"pg-mfoot\"><button type=\"button\" class=\"pg-btn pg-btn-ghost\" onclick=\"AdminPagesJs.closeModal()\">Close</button></div>");
            response.SetElementContents("pg-mbody", sb.ToString());
            response.ExecuteScript("AdminPagesJs.openModal();");
            return response;
        }

        public async Task<ApiResponse> Restore()
        {
            ApiResponse response = new ApiResponse();
            Ctx? c = await Auth.Check(GetDataValue("tok"));
            if (c == null)
            {
                response.ExecuteScript("AdminPagesJs.expired();");
                return response;
            }
            if (!Access.Can(c.U, AreaKey, 'E'))
            {
                return await Refuse(response, c, "restore page", GetDataValue("page"), "You can look but not change pages.");
            }
            PageSpec? p = Blocks.PageOf(GetDataValue("page"));
            int i = int.TryParse(GetDataValue("vi"), out int iv) ? iv : -1;
            SiteData? done = null;
            if (p != null)
            {
                using (await Store.Hold())
                {
                    SiteData site = await Store.LoadSite();
                    PageDoc doc = site.Pages[p.Key];
                    if (i >= 0 && i < doc.Versions.Count)
                    {
                        PageVersion v = doc.Versions[i];
                        doc.Versions.RemoveAt(i);
                        doc.Versions.Insert(0, new PageVersion { When = doc.Saved == default ? DateTime.UtcNow.AddMinutes(-1) : doc.Saved, By = doc.By, Blocks = doc.Blocks });
                        if (doc.Versions.Count > 5)
                        {
                            doc.Versions.RemoveRange(5, doc.Versions.Count - 5);
                        }
                        doc.Blocks = v.Blocks;
                        doc.Saved = DateTime.UtcNow;
                        doc.By = c.U.Id;
                        Blocks.Ensure(site);
                        await Store.SaveSite(site);
                        done = site;
                    }
                }
            }
            if (p == null || done == null)
            {
                return response;
            }
            await Store.Log(c.U.Id, "restore page", p.Name, true);
            response.SetElementContents("pg-canvas", Canvas(done, p, true));
            response.ExecuteScript("AdminPagesJs.shown('" + p.Key + "');AdminPagesJs.saved('Version restored. It is live on the website.');");
            return response;
        }
    }
}
