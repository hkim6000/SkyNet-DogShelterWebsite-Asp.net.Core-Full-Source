var AdminEventsJs = (function () {

    var ttimer = null;
    var BASE = location.pathname.replace(/[^\/]*$/, '');

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('AdminEvents/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('ez-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('ez-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ez-open');
        }, 3200);
    }

    function openNav() {
        el('ez-nav').classList.add('ez-open');
        el('ez-scrim').classList.add('ez-open');
    }

    function closeNav() {
        el('ez-nav').classList.remove('ez-open');
        el('ez-scrim').classList.remove('ez-open');
    }

    function ago(d) {
        var s = (Date.now() - d.getTime()) / 1000;
        if (s < 60) {
            return 'Just now';
        }
        if (s < 3600) {
            var m = Math.floor(s / 60);
            return m + (m === 1 ? ' min ago' : ' mins ago');
        }
        if (s < 86400) {
            var h = Math.floor(s / 3600);
            return h + (h === 1 ? ' hour ago' : ' hours ago');
        }
        return d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
    }

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function times(root) {
        var list = (root || document).querySelectorAll('time[data-t]:not([data-x])');
        for (var i = 0; i < list.length; i++) {
            var t = list[i];
            t.setAttribute('data-x', '1');
            var d = new Date(t.getAttribute('datetime'));
            if (isNaN(d.getTime())) {
                continue;
            }
            var mode = t.getAttribute('data-t');
            if (mode === 'ago') {
                t.textContent = ago(d);
                t.title = d.toLocaleString();
            } else if (mode === 'hm') {
                t.textContent = d.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
            } else if (mode === 'full') {
                t.textContent = d.toLocaleString(undefined, { weekday: 'short', day: 'numeric', month: 'long', year: 'numeric', hour: 'numeric', minute: '2-digit' });
            } else if (mode === 'day') {
                t.textContent = d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
            }
        }
    }

    var stimer = null;
    var otimer = null;

    function api(method, list) {
        list = list || [];
        list.push({ key: 'tok', vlu: getCookie('wd_tok') });
        call(method, list);
    }

    function expired() {
        setCookie('wd_tok', '', 0);
        location.replace('AdminLogin');
    }

    function signOut() {
        api('SignOut', []);
    }

    function out() {
        setCookie('wd_tok', '', 0);
        location.href = 'AdminLogin';
    }

    function openModal() {
        el('ez-modal').classList.add('ez-open');
        times(el('ez-mbody'));
        var f = el('ez-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('ez-modal');
        if (m) {
            m.classList.remove('ez-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('ez-per')) {
            el('ez-per').value = k;
        }
        if (k !== 'range') {
            if (el('ez-from')) {
                el('ez-from').value = '';
            }
            if (el('ez-to')) {
                el('ez-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('ez-per') }, { key: 'pfrom', vlu: val('ez-from') }, { key: 'pto', vlu: val('ez-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('ez-main'));
        if (window.MutationObserver && el('ez-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('ez-main'));
                }, 30);
            }).observe(el('ez-main'), { childList: true, subtree: true });
        }
        if (window.AdminEventsJsloaded) {
            window.AdminEventsJsloaded();
        }
    }

    var askBox = null;
    var askDone = null;
    var askCheck = null;
    var askBack = null;

    function askParts() {
        return { h: askBox.querySelector('h2'), p: askBox.querySelector('p'), i: askBox.querySelector('input'), e: askBox.querySelector('em'), b: askBox.querySelectorAll('button') };
    }

    function askClose(ok) {
        if (!askBox || !askBox.classList.contains('ez-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('ez-open');
        askDone = null;
        askCheck = null;
        if (askBack && askBack.focus && document.body.contains(askBack)) {
            askBack.focus();
        }
        askBack = null;
        if (ok && f) {
            f(v);
        }
    }

    function askOk() {
        var x = askParts();
        if (askCheck) {
            var m = askCheck(x.i.value.trim());
            if (m) {
                x.e.textContent = m;
                x.i.focus();
                return;
            }
        }
        askClose(true);
    }

    function askBuild() {
        askBox = document.createElement('div');
        askBox.className = 'ez-ask';
        askBox.innerHTML = '<div class="ez-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="ez-err" role="alert"></em><div class="ez-askacts"><button type="button" class="ez-btn ez-btn-ghost ez-btn-sm"></button><button type="button" class="ez-btn ez-btn-sm"></button></div></div>';
        (el('ez-site') || document.body).appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () {
            askClose(false);
        });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () {
            x.e.textContent = '';
        });
        askBox.addEventListener('mousedown', function (e) {
            if (e.target === askBox) {
                askClose(false);
            }
        });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('ez-open')) {
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                askClose(false);
            } else if (e.key === 'Enter' && e.target !== x.b[0]) {
                e.preventDefault();
                e.stopPropagation();
                askOk();
            } else if (e.key === 'Tab') {
                var f = [x.i, x.b[0], x.b[1]].filter(function (n) {
                    return n.offsetParent !== null;
                });
                var at = f.indexOf(document.activeElement);
                e.preventDefault();
                f[(at + (e.shiftKey ? f.length - 1 : 1)) % f.length].focus();
            }
        }, true);
    }

    function ask(o, done) {
        if (!askBox) {
            askBuild();
        }
        var x = askParts();
        askBack = document.activeElement;
        askDone = done;
        askCheck = o.check || null;
        x.h.textContent = o.title || '';
        x.p.textContent = o.text || '';
        x.e.textContent = '';
        x.b[0].textContent = o.no || 'Cancel';
        x.b[1].textContent = o.ok || 'OK';
        x.b[1].className = 'ez-btn ez-btn-sm' + (o.danger ? ' ez-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('ez-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('ez-open');
        if (input) {
            x.i.focus();
            x.i.setSelectionRange(x.i.value.length, x.i.value.length);
        } else {
            x.b[o.danger ? 0 : 1].focus();
        }
    }

    function leave(go) {
        ask({ title: 'Leave without saving?', text: 'Your changes since the last save will be lost.', ok: 'Leave', no: 'Keep editing', danger: true }, go);
    }

    var pk = { kind: '', allow: '', multi: false, tag: '', done: null, page: 1, chosen: [] };
    var pkTimer = null;

    function pkBuild() {
        if (el('ez-pk')) {
            return;
        }
        var d = document.createElement('div');
        d.className = 'ez-pk';
        d.id = 'ez-pk';
        d.setAttribute('role', 'dialog');
        d.setAttribute('aria-modal', 'true');
        d.innerHTML = '<div class="ez-pkbox"><header><h2 id="ez-pkt">Choose from Media</h2><button type="button" class="ez-ibtn" aria-label="Close" onclick="AdminEventsJs.pkClose()">&times;</button></header><div class="ez-pkbar"><input type="search" id="ez-pkq" placeholder="Search by name or tag" maxlength="60" oninput="AdminEventsJs.pkSearch()"><select id="ez-pkkind" aria-label="Type" onchange="AdminEventsJs.pkList(1)"><option value="">Photos and videos</option><option value="photo">Photos</option><option value="video">Videos</option></select><select id="ez-pktag" aria-label="Tag" onchange="AdminEventsJs.pkList(1)"><option value="">Any tag</option></select><label class="ez-btn ez-btn-sm ez-btn-ghost ez-pkup" id="ez-pkupl">Upload a photo<input type="file" id="ez-pkfile" accept="image/jpeg,image/png,image/webp" onchange="AdminEventsJs.pkUpload(this)"></label></div><em class="ez-err ez-pkerr" id="ez-pkerr" role="alert"></em><div class="ez-pkgrid" id="ez-pkgrid"></div><div class="ez-pkmore" id="ez-pkmore"></div><footer><span id="ez-pkn"></span><button type="button" class="ez-btn ez-btn-ghost" onclick="AdminEventsJs.pkClose()">Cancel</button><button type="button" class="ez-btn ez-btn-accent" id="ez-pkuse" onclick="AdminEventsJs.pkUse()">Add</button></footer></div>';
        (el('ez-site') || document.body).appendChild(d);
        d.addEventListener('mousedown', function (e) {
            if (e.target === d) {
                pkClose();
            }
        });
        d.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                e.stopPropagation();
                pkClose();
            }
        });
    }

    function pickMedia(opts) {
        pkBuild();
        pk.allow = opts.allow || '';
        pk.multi = !!opts.multi;
        pk.tag = opts.tag || '';
        pk.done = opts.done;
        pk.chosen = [];
        el('ez-pkt').textContent = opts.title || 'Choose from Media';
        el('ez-pkq').value = '';
        el('ez-pkerr').textContent = '';
        var k = el('ez-pkkind');
        k.value = pk.allow === 'photo' ? 'photo' : '';
        k.disabled = pk.allow === 'photo' || pk.allow === 'video';
        if (pk.allow === 'video') {
            k.value = 'video';
        }
        el('ez-pkupl').hidden = pk.allow === 'video';
        el('ez-pkuse').hidden = !pk.multi;
        pkCount();
        el('ez-pk').classList.add('ez-open');
        pkList(1);
        setTimeout(function () {
            el('ez-pkq').focus();
        }, 30);
    }

    function pkList(pg) {
        pk.page = pg || 1;
        api('MediaPick', [{ key: 'pkind', vlu: val('ez-pkkind') }, { key: 'pallow', vlu: pk.allow }, { key: 'pterm', vlu: val('ez-pkq') }, { key: 'ptag', vlu: val('ez-pktag') }, { key: 'pctx', vlu: pk.tag }, { key: 'ppg', vlu: String(pk.page) }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(function () {
            pkList(1);
        }, 300);
    }

    function pkShown(html, more, tags, append) {
        var g = el('ez-pkgrid');
        if (append) {
            g.insertAdjacentHTML('beforeend', html);
        } else {
            g.innerHTML = html;
            g.scrollTop = 0;
        }
        el('ez-pkmore').innerHTML = more ? '<button type="button" class="ez-btn ez-btn-sm ez-btn-ghost" onclick="AdminEventsJs.pkList(' + (pk.page + 1) + ')">Show more</button>' : '';
        if (tags !== null) {
            var t = el('ez-pktag');
            var cur = t.value;
            t.innerHTML = '<option value="">Any tag</option>' + tags.map(function (x) {
                return '<option value="' + esc(x) + '">' + esc(x) + '</option>';
            }).join('');
            t.value = cur;
        }
        var b = g.querySelectorAll('.ez-pki');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('ez-on', pk.chosen.some(function (c) {
                return c.src === b[i].getAttribute('data-src');
            }));
        }
    }

    function pkItem(b) {
        return { src: b.getAttribute('data-src'), thumb: b.getAttribute('data-thumb'), kind: b.getAttribute('data-kind'), title: b.getAttribute('data-title') };
    }

    function pkToggle(b) {
        var it = pkItem(b);
        if (!pk.multi) {
            var f = pk.done;
            pkClose();
            if (f) {
                f([it]);
            }
            return;
        }
        var at = -1;
        pk.chosen.forEach(function (c, i) {
            if (c.src === it.src) {
                at = i;
            }
        });
        if (at >= 0) {
            pk.chosen.splice(at, 1);
        } else {
            pk.chosen.push(it);
        }
        b.classList.toggle('ez-on', at < 0);
        pkCount();
    }

    function pkCount() {
        var n = pk.chosen.length;
        el('ez-pkn').textContent = pk.multi ? (n ? n + ' selected' : 'Tap to select one or more') : 'Tap one to use it';
        el('ez-pkuse').disabled = n === 0;
        el('ez-pkuse').textContent = n > 1 ? 'Add ' + n : 'Add';
    }

    function pkUse() {
        var f = pk.done;
        var list = pk.chosen.slice();
        pkClose();
        if (f && list.length) {
            f(list);
        }
    }

    function pkClose() {
        var d = el('ez-pk');
        if (d) {
            d.classList.remove('ez-open');
        }
        pk.done = null;
    }

    function pkShrink(img, max, q) {
        var r = Math.min(1, max / Math.max(img.naturalWidth, img.naturalHeight));
        var c = document.createElement('canvas');
        c.width = Math.round(img.naturalWidth * r);
        c.height = Math.round(img.naturalHeight * r);
        c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
        var d = c.toDataURL('image/webp', q);
        return d.indexOf('data:image/webp') === 0 ? d : c.toDataURL('image/jpeg', q);
    }

    function pkUpload(input) {
        var f = input.files && input.files[0];
        el('ez-pkerr').textContent = '';
        if (!f) {
            return;
        }
        if (!/^image\/(jpeg|png|webp)$/.test(f.type) || f.size > 30 * 1024 * 1024) {
            el('ez-pkerr').textContent = 'Choose a JPG, PNG or WebP photo under 30 MB.';
            input.value = '';
            return;
        }
        var url = URL.createObjectURL(f);
        var img = new Image();
        img.onload = function () {
            URL.revokeObjectURL(url);
            var full = pkShrink(img, 1600, 0.86);
            var thumb = pkShrink(img, 640, 0.8);
            el('ez-pkupl').classList.add('ez-busy');
            api('MediaUp', [{ key: 'full', vlu: full }, { key: 'thumb', vlu: thumb }, { key: 'pname', vlu: f.name.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ').substring(0, 80) }, { key: 'pctx', vlu: pk.tag }, { key: 'pw', vlu: String(img.naturalWidth) }, { key: 'ph', vlu: String(img.naturalHeight) }]);
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            el('ez-pkerr').textContent = 'That image could not be read.';
        };
        img.src = url;
        input.value = '';
    }

    function pkUploaded(ok, msg) {
        el('ez-pkupl').classList.remove('ez-busy');
        if (!ok) {
            el('ez-pkerr').textContent = msg;
            return;
        }
        el('ez-pkq').value = '';
        el('ez-pkkind').value = pk.allow === 'photo' ? 'photo' : '';
        el('ez-pktag').value = '';
        pkList(1);
        toast('Photo added to Media.');
    }

    var opened = '';
    var pic = '';
    var picThumb = '';
    var view = 'list';
    var anchor = '';

    window.AdminEventsJsloaded = function () {
        var v = qs('view');
        if (v === 'week' || v === 'month') {
            setView(v);
        } else {
            list(1);
        }
        var id = qs('id');
        if (id) {
            edit(id === 'new' ? '' : id);
        }
    };

    function loadArgs() {
        return [];
    }

    function list(pg) {
        if (view === 'list') {
            api('List', [{ key: 'eq', vlu: val('ez-q') }, { key: 'eshow', vlu: val('ez-eshow') }, { key: 'ekind', vlu: val('ez-ekind') }, { key: 'pg', vlu: String(pg || 1) }]);
        } else {
            cal(0);
        }
    }

    function cal(step) {
        api('Cal', [{ key: 'cview', vlu: view }, { key: 'cdate', vlu: anchor }, { key: 'cstep', vlu: String(step) }, { key: 'ekind', vlu: val('ez-ekind') }]);
    }

    function setView(v) {
        view = v;
        var b = document.querySelectorAll('#ez-vchips .ez-chip');
        for (var i = 0; i < b.length; i++) {
            var on = b[i].getAttribute('data-v') === v;
            b[i].classList.toggle('ez-act', on);
            b[i].setAttribute('aria-pressed', on ? 'true' : 'false');
        }
        el('ez-calnav').hidden = v === 'list';
        el('ez-listbox').hidden = v !== 'list';
        el('ez-calbox').hidden = v === 'list';
        el('ez-qw').hidden = v !== 'list';
        el('ez-showw').hidden = v !== 'list';
        try {
            history.replaceState(null, '', 'AdminEvents' + (v === 'list' ? '' : '?view=' + v));
        } catch (e) {
        }
        list(1);
    }

    function calSet(a, label) {
        anchor = a;
        el('ez-callabel').textContent = label;
    }

    function today() {
        anchor = '';
        cal(0);
    }

    function newOn(day) {
        api('Form', [{ key: 'eid', vlu: '' }, { key: 'eday', vlu: day }]);
    }

    function dayClick(e, day) {
        if (e.target.closest('button')) {
            return;
        }
        if (el('ez-calbox').getAttribute('data-add') === '1') {
            newOn(day);
        }
    }

    function edit(id) {
        api('Form', [{ key: 'eid', vlu: id || '' }]);
    }

    function pane(k) {
        var tabs = document.querySelectorAll('#ez-mbody .ez-tab');
        for (var i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('ez-act', tabs[i].getAttribute('data-p') === k);
        }
        var ps = document.querySelectorAll('#ez-mbody .ez-panes > div');
        for (var j = 0; j < ps.length; j++) {
            ps[j].classList.toggle('ez-act', ps[j].getAttribute('data-p') === k);
        }
    }

    function opened2(tab) {
        var d = el('ez-edata');
        opened = d ? d.getAttribute('data-id') : '';
        pic = d ? d.getAttribute('data-pic') : '';
        picThumb = d ? d.getAttribute('data-pthumb') : '';
        drawPic();
        openModal();
        if (tab) {
            pane(tab);
        }
    }

    function drawPic() {
        var box = el('ez-cover');
        if (!box) {
            return;
        }
        box.innerHTML = pic ? '<img src="' + esc(picThumb || pic) + '" alt="Event picture">' : 'No picture';
        var c = el('ez-nopic');
        if (c) {
            c.hidden = !pic;
        }
    }

    function choosePic() {
        pickMedia({ allow: 'photo', tag: 'events', title: 'Choose the event picture', done: function (list) {
            pic = list[0].src;
            picThumb = list[0].thumb;
            drawPic();
        } });
    }

    function noPic() {
        pic = '';
        picThumb = '';
        drawPic();
    }

    function save() {
        var keys = ['etitle', 'ekind2', 'edate', 'estart', 'eend', 'eplace', 'ecap', 'etext'];
        var list2 = keys.map(function (k) {
            return { key: k, vlu: el('ez-' + k) ? el('ez-' + k).value : '' };
        });
        list2.push({ key: 'epub', vlu: el('ez-epub') && el('ez-epub').checked ? '1' : '0' });
        list2.push({ key: 'eid', vlu: opened });
        list2.push({ key: 'epic', vlu: pic });
        api('Save', list2);
    }

    function saved(id, isNew) {
        closeModal();
        list(1);
        toast(isNew ? 'Event added.' : 'Saved.');
        try {
            history.replaceState(null, '', 'AdminEvents');
        } catch (e) {
        }
    }

    function fail(msg) {
        el('ez-e-save').textContent = msg;
        pane('det');
    }

    function arrive(rid, box) {
        api('Arrive', [{ key: 'eid', vlu: opened }, { key: 'rid', vlu: rid }, { key: 'on', vlu: box.checked ? '1' : '0' }]);
    }

    function arrived(n, total) {
        var a = el('ez-arr');
        if (a) {
            a.textContent = String(n);
        }
        list(1);
    }

    function drop(rid, name) {
        ask({ title: 'Cancel the reservation for ' + name + '?', text: 'Their places become free again.', ok: 'Cancel reservation', no: 'Keep it', danger: true }, function () {
            api('DropRsvp', [{ key: 'eid', vlu: opened }, { key: 'rid', vlu: rid }]);
        });
    }

    function remove(name, n) {
        ask({ title: 'Delete ' + name + '?', text: n > 0 ? 'The event and its ' + n + ' reservations are removed. Let the guests know first.' : 'The event is removed from the website and the admin.', ok: 'Delete event', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'eid', vlu: opened }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Event deleted.');
    }

    function reveal() {
        if (!getCookie('wd_tok')) {
            location.replace('AdminLogin');
            return;
        }
        api('Load', loadArgs());
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
                closeModal();
            }
        });
        document.addEventListener('input', function (e) {
            var id = e.target && e.target.id ? e.target.id.replace('ez-', 'ez-e-') : '';
            if (id && el(id)) {
                el(id).textContent = '';
            }
        });
    }

    return {
        reveal: reveal,
        toast: toast,
        openNav: openNav,
        closeNav: closeNav,
        signOut: signOut,
        out: out,
        expired: expired,
        openModal: openModal,
        closeModal: closeModal,
        per: per,
        search: search,
        loaded: loaded,
        list: list,
        cal: cal,
        setView: setView,
        calSet: calSet,
        today: today,
        newOn: newOn,
        dayClick: dayClick,
        edit: edit,
        pane: pane,
        opened: opened2,
        choosePic: choosePic,
        noPic: noPic,
        save: save,
        saved: saved,
        fail: fail,
        arrive: arrive,
        arrived: arrived,
        drop: drop,
        remove: remove,
        deleted: deleted,
        pickMedia: pickMedia,
        pkList: pkList,
        pkSearch: pkSearch,
        pkShown: pkShown,
        pkToggle: pkToggle,
        pkUse: pkUse,
        pkClose: pkClose,
        pkUpload: pkUpload,
        pkUploaded: pkUploaded
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminEventsJs.reveal);
} else {
    AdminEventsJs.reveal();
}
