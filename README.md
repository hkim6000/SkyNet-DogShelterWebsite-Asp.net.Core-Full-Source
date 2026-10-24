# DogShelter — SkyNet Dog Rescue Template

A responsive website for a nonprofit dog rescue: adoptable dogs with photo and video galleries, adoption applications, foster and volunteer sign-ups, events with reservations, donation pledges, a page editor, a media library and a full staff admin, built for **ASP.NET Core (.NET 10)** on the **SkyNet Framework**.
It is universal: rename the shelter, set your town, adoption fees, donation amounts, visiting hours and time zone, and the whole site follows. Data is stored in JSON files, with no database.
Free and open source. **100% AI-driven coding — built by Claude.**

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/TheSkyLite.SkyNet?label=TheSkyLite.SkyNet)](https://www.nuget.org/packages/TheSkyLite.SkyNet)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AI-driven](https://img.shields.io/badge/code-100%25%20AI--driven-9A5B2E)](#100-ai-driven-coding)

**Live demo:** https://www.theskylite.com/DogShelter

![DogShelter home page](docs/screenshot-home.png)

> **Fictional rescue.** Walnut Dog Rescue, the town of Hollis Ferry and every dog, person, gift and address are invented. The dog portraits, scenes and sample videos are made in code. There are no stock images and no payment service.

---

## Features

### Rescue website (10 pages)

- **Every page is built from blocks** that the staff edit in **Pages**: page top, text, picture and text, cards, steps, checklists, questions, quote, picture or video, call to action, plus the blocks filled from the data (dogs, events, forms).
- **Home:**
  - a hero with a background **video**, a **dog finder** (size, age, good with) and live counts: dogs waiting, adopted this year, in foster homes;
  - four ways to help (adopt, foster, volunteer, donate), featured dogs, dogs who need a home soon;
  - the next events, happy tails, the shelter's story and a donation band.
- **Open for visits / closed now** bar on every page, worked out in the shelter's own time zone from the visiting hours.
- **Adopt a dog (Dogs):**
  - search by name or breed; size, age group (puppy, adult, senior), sex, good with kids, dogs or cats, energy;
  - sort by newest, oldest, waiting longest or youngest; show or hide dogs with an adoption pending; paging;
  - badges for new, urgent, bonded pair, in foster and adoption pending, and a Video mark; the URL keeps the search.
- **Saved dogs:** a heart on every card and dog page. Saved dogs stay in the browser (no account), with a count in the header and their own view.
- **Dog page:**
  - a photo and **video** gallery and a full-screen **lightbox** (arrow keys, swipe, thumbnails; videos play in it);
  - age, sex, size, weight, color, energy, good with kids, dogs and cats, house-trained, vaccinated, spayed or neutered, microchipped, care notes, adoption fee;
  - the story and personality, a bonded-pair note, **Apply to adopt**, **Ask about this dog**, **Sponsor** and **Share**;
  - similar dogs. Adopted dogs stay online with their happy tail.
- **How to adopt:** the steps, fees for puppies, adults and seniors, questions and answers, and the **adoption application** (pick a dog or let the team suggest one; home, yard, own or rent, children, other pets, hours alone, experience).
- **Foster:** what fostering involves and a sign-up form (home, yard, pets, children, sizes, how long, medical cases, puppies).
- **Volunteer:** the jobs and a **simple sign-up with availability** (jobs and weekday or weekend mornings, afternoons and evenings, 18 or older).
- **Events:** open houses, adoption days, classes, fundraisers and orientations. **Reserve a place** with a party size; places left update live and full events close.
- **Donate:** one-time or monthly, preset or own amount, fund (where needed most, medical, sponsor a dog, food and supplies), dedication, private name, and how the gift will be sent (check, at the shelter, bank transfer). Gifts are **pledges**: the staff mark them received when the money arrives. Plus a wish list.
- **Happy tails:** adopted dogs with their stories.
- **Contact:** address, phone, visiting hours and a message form with topics. **News by email** sign-up in every footer.
- **Light and dark theme.** Responsive layouts for desktop, tablet and phone.

### Staff admin (15 pages)

- **Sign in** with the sample account (filled in on the sign-in page) or your own password (PBKDF2). Failed attempts are throttled.
- **Dashboard:** dogs available and in care, new applications, gifts received this month, volunteers, the next event; the newest applications, coming events, what needs attention (medical holds, long stays, foster stays past their date, new foster homes and volunteers, old pledges, new messages) and the latest gifts.
- **Dogs:** search, filters, sort and paging. The editor has tabs:
  - **Details:** name, breed, sex, birth date, size, weight, color, energy, good with, health checks, fee, bonded partner, intake date, status, featured, urgent;
  - **Story**, personality and happy tail;
  - **Photos and videos:** add from Media (upload new photos right in the picker), reorder; the first photo is the cover;
  - **Health:** vaccines, vet visits, medicine and surgery;
  - **History** of status changes (added automatically when you save).
- **Applications:** every adoption application (18 samples). Progress (New, Reviewing, Home visit, Withdrawn), book the home visit, assign a reviewer, notes. **Approve** marks the dog Adoption pending; **Decline** or **Withdraw** puts it back; **Mark adopted** moves the dog to Happy tails and notes the other open applications.
- **Fosters:** foster homes (approve, pause, decline, notes) and **Dogs in foster**: place a dog with an approved home, set the planned end, end the stay. Stays past their date are flagged.
- **Volunteers:** jobs and free times at a glance, filters by job and time, status, an **hours log** with totals for the month and the year, notes. **Add a volunteer** who signed up by phone or at an event, and **edit** anyone's details.
- **Events:** **List, Week and Month** views; click an empty day to add an event on that date. Add and edit events with a picture from Media, places or drop-in, show on the website or keep as a draft. **Guests:** reservations, check-in on the day, cancel.
- **Donations:** totals for the month and the year, open pledges, monthly donors; filters by status, fund, method and period; **Mark received**, cancel, **Record a gift** that came in by mail or at an event, notes and **Export CSV**.
- **Messages:** questions about dogs and contact-form messages. Reply by email, mark replied or closed, notes.
- **Pages:** edit every public page in place, the way it looks:
  - click any text to change it; bold, italic, links and lists in the longer texts;
  - change a picture or video from Media; drag blocks by the handle or use the arrows; hide a block without deleting it;
  - add blocks (page top, text, picture and text, cards, steps, checklists, questions, quote, picture or video, call to action), add, move and remove cards, steps and questions;
  - blocks filled from the data (dogs, events, forms) can be moved, retitled or hidden but not removed;
  - **Save page** puts it live; the last five versions are kept and can be restored.
- **Media:** one library for photos and videos (120 samples):
  - upload photos (resized in the browser to 1600 px) and videos (MP4 or WebM up to 60 MB, sent in 1 MB parts, with a cover frame taken in the browser);
  - name, description for screen readers and tags; search, filter by type, tag and used or not;
  - **Used on** shows the dogs, events and pages that use each item; items in use cannot be deleted.
- **Settings:** shelter details, accent color, theme, adoption fees, donation amounts, pledge instructions, wish list, visiting hours, time zone, 24-hour clock.
- **Users:** a permission matrix for each person. **Activity log** and **My account.**
- **Every list** has a search box with suggestions as you type, filters and paging.

### Permissions

| Area | Rights | Extra right |
|---|---|---|
| Dogs | View, Edit | **Publish** (status, featured, urgent, delete) |
| Applications | View, Edit | **Decide** (approve, decline, mark adopted, delete) |
| Fosters, Volunteers, Events, Messages, Pages | View, Edit | |
| Donations | View, Edit | **Export** (CSV export, delete) |
| Media | View, Edit | **Delete** |
| Activity log | View | |
| Users, Settings | shelter managers only | |

- Without **Publish**, new dogs stay in Intake until someone with the right puts them on the website.
- The menu shows only the areas a person can see, with counts of new items. Every check runs on the server; a refused request is written to the activity log.

### Sign in

| User name | Password | Role |
|---|---|---|
| `admin` | `Walnut2026` | Shelter manager, every area |
| `maria` | `Staff2026` | Adoption coordinator: dogs, applications (decide), messages |
| `tom` | `Staff2026` | Foster coordinator: dogs, fosters, messages |
| `aisha` | `Staff2026` | Volunteer coordinator: volunteers, events, messages |
| `jordan` | `Staff2026` | Development: donations (export), events, activity log |
| `ben`, `lucy` | `Staff2026` | Kennel staff: dogs |

- The sign-in page fills in the shelter manager and lists the staff sign-ins below the button.
- Change the shelter manager's password in **My account**: the filled-in fields and the hint go away.
- Change the staff passwords in **Users**, and add your own team.
- Deleting `data/staff.json` brings back the `admin` sign-in only.

![Staff sign in](docs/screenshot-admin-login.png)

![Desktop, tablet and phone](docs/screenshot-responsive.png)

![Adopt a dog](docs/screenshot-dogs.png)

![Dog page](docs/screenshot-dog.png)

![Photo gallery](docs/screenshot-gallery.png)

![Adoption application](docs/screenshot-adopt.png)

![Events](docs/screenshot-events.png)

![Donate](docs/screenshot-donate.png)

![Dashboard](docs/screenshot-admin-dashboard.png)

![Dogs](docs/screenshot-admin-dogs.png)

![Dog editor: photos and videos](docs/screenshot-admin-dog.png)

![Applications](docs/screenshot-admin-applications.png)

![An application](docs/screenshot-admin-application.png)

![Dogs in foster](docs/screenshot-admin-fosters.png)

![A volunteer and the hours log](docs/screenshot-admin-volunteer.png)

![Event guests and check-in](docs/screenshot-admin-event.png)

![Events: month view](docs/screenshot-admin-calendar.png)

![Donations](docs/screenshot-admin-donations.png)

![Pages: edit the website in place](docs/screenshot-admin-pages.png)

![Media library](docs/screenshot-admin-media.png)

![Users: permissions by area](docs/screenshot-admin-permissions.png)

![Settings: visiting hours](docs/screenshot-admin-settings.png)

- **Painted portraits:** 34 dogs in 3 poses each and 10 scenes, rendered in code by Claude as 224 WebP files, plus 8 short sample videos made from the paintings.
- **No payment service:** donations are pledges, so there are no card details, keys or fees to manage.
- **No front-end build:** plain HTML, CSS and vanilla JavaScript; no npm, bundler or SPA framework.
- **No database:** everything lives in JSON files under `data/`.

---

## Getting started

**Requirements:** .NET 10 SDK and Visual Studio (or any editor with the `dotnet` CLI).

```bash
git clone https://github.com/hkim6000/SkyNet-DogShelterWebsite-Asp.net.Core-Full-Source.git
cd SkyNet-DogShelterWebsite-Asp.net.Core-Full-Source
dotnet run
```

Or open `DogShelter.csproj` in Visual Studio and press **F5**.
The SkyNet package (`TheSkyLite.SkyNet` 1.0.6) restores automatically from NuGet.
The app opens on **Home**, the startup page set in `appConfig/application.cfg`.

**Admin:** click **Staff sign in** in the footer (or open `/AdminLogin`) and press **Sign in**. See [Sign in](#sign-in).

**Dates:** the sample dogs, applications, events, gifts and messages carry dates relative to the day you first run the site, so "new" badges, coming events and days in care look right. This happens once; after that dates stay as they are.

---

## Make it yours

1. **Settings:** name, town, address, phone, email, charity line, accent color, adoption fees, donation amounts, pledge instructions, wish list, visiting hours and time zone.
2. **Pages:** rewrite the text of every page, change the pictures and videos, reorder or hide blocks and add your own. `{name}` and `{town}` in a text show the values from Settings.
3. **Media:** upload your own photos and videos, then delete the samples you no longer use.
4. **Dogs:** replace the sample dogs with your own. Add photos and videos from Media, write the story and publish.
5. **Events:** add your open houses and classes with a picture and the number of places.
6. **Sample people:** delete the sample applications, foster homes, volunteers, gifts and messages in the admin, or start from a `data/site.json` with those lists empty.
7. **Files:** uploaded photos go to `images/uploads/full` (1600 px) and `images/uploads/thumbs` (640 px), videos to `images/uploads/video` with their cover in `images/uploads/thumbs`. The sample portraits live in `images/dogs/full` and `images/dogs/thumbs`, the scenes in `images/scenes`, the sample videos in `images/media`.

---

## Going live

1. **Change the sample passwords:** your own in My account, the staff in Users. Remove the sample staff you don't need and add your team.
2. **Give the site write access** to these folders. On IIS, grant the application pool identity (for example `IIS AppPool\DogShelter`) **Modify** permission on:

   | Folder | What is written |
   |---|---|
   | `data/` | `site.json`, `staff.json`, `activity.json`, `subscribers.json` |
   | `images/uploads/` | uploaded photos and videos |
   | `temp/` | staff sessions (`temp/admin`) and video uploads in progress (`temp/uploads`) |

3. **Block private files.** Open `https://<your-site>/data/staff.json` in a browser: it must answer 404. If your host serves it, block the `data/` folder in the server settings.
4. **Use HTTPS.** Session tokens travel in cookies and in each request.
5. **Never commit** your live `data/staff.json`, `data/activity.json`, `data/subscribers.json` or `temp/`.
6. **Check your local rules** for charitable solicitation, donation receipts and storing the contact details of adopters, fosters, volunteers and donors.

> **Redeploying:** publishing copies the files in `data/` from your project over the live ones. Download the live `data/` folder first (or exclude it from the publish) so you keep your dogs and applications. Turn off "Remove additional files at destination" so `temp/` and `images/uploads/` are not deleted.

Staff sessions last 8 hours and renew while you work. Changing a staff password signs that person out everywhere.

---

## Project structure

```
DogShelter/
├── appConfig/application.cfg        app settings
├── codes/
│   ├── Models/DogShelterModel.cs    data DTOs (Settings, Pet, Application, FosterHome, Placement, VolunteerInfo, Event, Donation, Message, Staff, ...)
│   ├── Core/Store.cs                JSON storage, sample account, time zone, one-time date shift
│   ├── Core/Access.cs               areas and rights, passwords
│   ├── Core/Auth.cs                 staff sessions, admin menu
│   ├── Core/Ui.cs                   shared HTML helpers, statuses, ages, media, period filter, paging, CSV
│   ├── Core/Blocks.cs               page blocks: the catalog, text cleaning, checks before saving, where media is used
│   ├── Home.cs  Dogs.cs  Dog.cs  Adopt.cs  Foster.cs  Volunteer.cs  Events.cs  Donate.cs  Stories.cs  Contact.cs
│   └── AdminLogin.cs  Admin.cs  AdminDogs.cs  AdminApplications.cs  AdminFosters.cs  AdminVolunteers.cs  AdminEvents.cs
│       AdminDonations.cs  AdminMessages.cs  AdminPages.cs  AdminMedia.cs  AdminSettings.cs  AdminUsers.cs  AdminActivity.cs  AdminAccount.cs
├── htmls/  scripts/  styles/        page markup, JavaScript and CSS
├── data/
│   ├── site.json                    settings, dogs, applications, foster homes and stays, volunteers, events, gifts, messages, media, page blocks
│   ├── staff.json                   users, password hashes and rights (sample shelter manager and staff)
│   ├── activity.json                the activity log (latest 5,000)
│   └── subscribers.json             news-by-email sign-ups
├── images/dogs/full/  dogs/thumbs/  painted portraits (1600 px and 640 px, WebP)
├── images/scenes/                   painted scenes for the hero, pages and events (WebP)
├── images/media/                    sample videos (MP4) and their covers
├── images/uploads/                  photos and videos uploaded in the admin (created at run time)
├── temp/                            staff sessions and video uploads in progress (created at run time)
└── Program.cs
```

### One page = four files, one name

| File | Holds |
|---|---|
| `codes/Dogs.cs` | the page class (`: WebPage`) and its server methods |
| `htmls/Dogs.html` | markup with `{plhd_*}` placeholders |
| `scripts/Dogs.js` | one IIFE namespace, `DogsJs` |
| `styles/Dogs.css` | styles, every class prefixed (`dg-`) |

---

## SkyNet in action

The browser calls a C# method, the method returns an `ApiResponse`, and only those parts of the page change.

```js
// scripts/AdminApplications.js
api('Decide', [{ key: 'aid', vlu: current }, { key: 'ast2', vlu: 'approved' }]);
```

```csharp
// codes/AdminApplications.cs
d.Status = "hold";
d.History.Add(new DogEvent { When = today, Event = "Adoption pending", By = c.U.Id });
await Store.SaveSite(site);
response.ExecuteScript("AdminApplicationsJs.changed('Approved. " + d.Name + " is now Adoption pending.');");
```

| Page | Requests |
|---|---|
| every public page | `Subscribe`, `Heart` |
| Dogs | `Search` |
| Dog / Foster / Volunteer / Donate / Contact | `Send` |
| Adopt | `Pick`, `Send` |
| Events | `Rsvp` |
| every admin page | `Load`, `SignOut` (AdminLogin: `SignIn`) |
| AdminDogs | `List`, `Form`, `Save`, `Delete`, `MediaPick`, `MediaUp` |
| AdminApplications | `List`, `Open`, `SetStatus`, `Decide`, `Assign`, `Visit`, `AddNote`, `Delete` |
| AdminFosters | `View`, `List`, `Stays`, `Open`, `SetStatus`, `Place`, `Stay`, `SaveStay`, `EndStay`, `AddNote`, `Delete` |
| AdminVolunteers | `List`, `Open`, `Form`, `SaveVol`, `SetStatus`, `LogHours`, `DropHours`, `AddNote`, `Delete` |
| AdminEvents | `List`, `Cal`, `Form`, `Save`, `Arrive`, `DropRsvp`, `Delete`, `MediaPick`, `MediaUp` |
| AdminDonations | `List`, `Export`, `Open`, `SetStatus`, `AddForm`, `AddGift`, `AddNote`, `Delete` |
| AdminMessages | `List`, `Open`, `SetStatus`, `AddNote`, `Delete` |
| AdminPages | `Show`, `AddBlock`, `Save`, `Versions`, `Restore`, `MediaPick`, `MediaUp` |
| AdminMedia | `List`, `Open`, `SaveInfo`, `Upload`, `Chunk`, `VideoDone`, `Remove` |
| AdminUsers / AdminSettings / AdminAccount / AdminActivity | `List`, `Form`, `Save` / `Save` / `Save` / `List` |

Learn more: [SkyNet Developer Guide](https://www.theskylite.com/documents/SkyNet_Developer_Guide.html)

---

## 100% AI-driven coding

Every file in this template was generated by Claude (Anthropic's AI) from short instructions, directed and reviewed by the author: C#, HTML, CSS, JavaScript, the painted portraits and scenes, and the data.
No line was written by hand.

| | |
|---|---|
| Pages | 25 (10 public, 15 admin) |
| Lines of code | ~32,000 (C#, HTML, CSS, JavaScript) |
| Images and video | 34 dogs × 3 portraits and 10 scenes (224 WebP), 8 sample videos |
| Sample dogs | 34 (26 waiting, 6 adopted, 2 in intake or medical care) |
| Sample applications / foster homes / volunteers | 18 / 10 / 16 |
| Sample events / gifts / messages | 6 / 62 / 12 |
| Media library / editable pages | 120 items / 8 pages, 33 blocks |
| Lines written by hand | 0 |

SkyNet's page model is small and predictable: one class, four files, `$ApiRequest` → `ApiResponse`.
With so few, consistent rules, AI can generate complete, working pages with very few errors.

---

## License

- **This template** (all source files, artwork and data in this repository): [MIT License](LICENSE). Free to use, modify and redistribute, including commercially.
- **SkyNet Framework** (`TheSkyLite.SkyNet` NuGet package): proprietary, free to use for eligible users including commercial use; see the license included in the package.

---

## Links

- Live demo: https://www.theskylite.com/DogShelter
- SkyNet Framework: https://www.theskylite.com
- More website templates: https://www.theskylite.com/SkyNetTemp
- NuGet package: https://www.nuget.org/packages/TheSkyLite.SkyNet
- SkyNet Agent (build your own site with Claude Code): https://github.com/hkim6000/SkyNet-Agent
- Template #1 — Beauty store: https://github.com/hkim6000/SkyNet-BeautyWebsite-Asp.net.Core-Full-Source
- Template #2 — Pharma company: https://github.com/hkim6000/SkyNet-PharmaWebsite-Asp.net.Core-Full-Source
- Template #3 — Travel blog: https://github.com/hkim6000/SkyNet-TravelWebsite-Asp.net.Core-Full-Source
- Template #4 — Fast food restaurant: https://github.com/hkim6000/SkyNet-FastFoodWebsite-Asp.net.Core-Full-Source
- Template #5 — Fine dining restaurant: https://github.com/hkim6000/SkyNet-FineDiningWebsite-Asp.net.Core-Full-Source
- Template #6 — Car dealership: https://github.com/hkim6000/SkyNet-CarDealerWebsite-Asp.net.Core-Full-Source
- Template #7 — Web comic: https://github.com/hkim6000/SkyNet-WebComicWebsite-Asp.net.Core-Full-Source
- Template #8 — Art gallery: https://github.com/hkim6000/SkyNet-ArtGalleryWebsite-Asp.net.Core-Full-Source
- Template #9 — Artist blog: https://github.com/hkim6000/SkyNet-ArtistBlogWebsite-Asp.net.Core-Full-Source
- Template #10 — Newspaper: https://github.com/hkim6000/SkyNet-SkyLitePostWebsite-Asp.net.Core-Full-Source
- Template #11 — Hotel: https://github.com/hkim6000/SkyNet-SkyHotelWebsite-Asp.net.Core-Full-Source
- Template #12 — Clinic: https://github.com/hkim6000/SkyNet-ClinicWebsite-Asp.net.Core-Full-Source
- Template #13 — Real estate agency: https://github.com/hkim6000/SkyNet-RealEstateWebsite-Asp.net.Core-Full-Source
- SkyNet project template: https://github.com/hkim6000/ASPNETCoreEmpty.SkyNet

© 2026 HC Kim
