var AdminDogsJs = (function () {

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
        $ApiRequest('AdminDogs/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('dz-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('dz-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('dz-open');
        }, 3200);
    }

    function openNav() {
        el('dz-nav').classList.add('dz-open');
        el('dz-scrim').classList.add('dz-open');
    }

    function closeNav() {
        el('dz-nav').classList.remove('dz-open');
        el('dz-scrim').classList.remove('dz-open');
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
        el('dz-modal').classList.add('dz-open');
        times(el('dz-mbody'));
        var f = el('dz-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('dz-modal');
        if (m) {
            m.classList.remove('dz-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('dz-per')) {
            el('dz-per').value = k;
        }
        if (k !== 'range') {
            if (el('dz-from')) {
                el('dz-from').value = '';
            }
            if (el('dz-to')) {
                el('dz-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('dz-per') }, { key: 'pfrom', vlu: val('dz-from') }, { key: 'pto', vlu: val('dz-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('dz-main'));
        if (window.MutationObserver && el('dz-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('dz-main'));
                }, 30);
            }).observe(el('dz-main'), { childList: true, subtree: true });
        }
        if (window.AdminDogsJsloaded) {
            window.AdminDogsJsloaded();
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
        if (!askBox || !askBox.classList.contains('dz-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('dz-open');
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
        askBox.className = 'dz-ask';
        askBox.innerHTML = '<div class="dz-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="dz-err" role="alert"></em><div class="dz-askacts"><button type="button" class="dz-btn dz-btn-ghost dz-btn-sm"></button><button type="button" class="dz-btn dz-btn-sm"></button></div></div>';
        (el('dz-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('dz-open')) {
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
        x.b[1].className = 'dz-btn dz-btn-sm' + (o.danger ? ' dz-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('dz-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('dz-open');
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
        if (el('dz-pk')) {
            return;
        }
        var d = document.createElement('div');
        d.className = 'dz-pk';
        d.id = 'dz-pk';
        d.setAttribute('role', 'dialog');
        d.setAttribute('aria-modal', 'true');
        d.innerHTML = '<div class="dz-pkbox"><header><h2 id="dz-pkt">Choose from Media</h2><button type="button" class="dz-ibtn" aria-label="Close" onclick="AdminDogsJs.pkClose()">&times;</button></header><div class="dz-pkbar"><input type="search" id="dz-pkq" placeholder="Search by name or tag" maxlength="60" oninput="AdminDogsJs.pkSearch()"><select id="dz-pkkind" aria-label="Type" onchange="AdminDogsJs.pkList(1)"><option value="">Photos and videos</option><option value="photo">Photos</option><option value="video">Videos</option></select><select id="dz-pktag" aria-label="Tag" onchange="AdminDogsJs.pkList(1)"><option value="">Any tag</option></select><label class="dz-btn dz-btn-sm dz-btn-ghost dz-pkup" id="dz-pkupl">Upload a photo<input type="file" id="dz-pkfile" accept="image/jpeg,image/png,image/webp" onchange="AdminDogsJs.pkUpload(this)"></label></div><em class="dz-err dz-pkerr" id="dz-pkerr" role="alert"></em><div class="dz-pkgrid" id="dz-pkgrid"></div><div class="dz-pkmore" id="dz-pkmore"></div><footer><span id="dz-pkn"></span><button type="button" class="dz-btn dz-btn-ghost" onclick="AdminDogsJs.pkClose()">Cancel</button><button type="button" class="dz-btn dz-btn-accent" id="dz-pkuse" onclick="AdminDogsJs.pkUse()">Add</button></footer></div>';
        (el('dz-site') || document.body).appendChild(d);
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
        el('dz-pkt').textContent = opts.title || 'Choose from Media';
        el('dz-pkq').value = '';
        el('dz-pkerr').textContent = '';
        var k = el('dz-pkkind');
        k.value = pk.allow === 'photo' ? 'photo' : '';
        k.disabled = pk.allow === 'photo' || pk.allow === 'video';
        if (pk.allow === 'video') {
            k.value = 'video';
        }
        el('dz-pkupl').hidden = pk.allow === 'video';
        el('dz-pkuse').hidden = !pk.multi;
        pkCount();
        el('dz-pk').classList.add('dz-open');
        pkList(1);
        setTimeout(function () {
            el('dz-pkq').focus();
        }, 30);
    }

    function pkList(pg) {
        pk.page = pg || 1;
        api('MediaPick', [{ key: 'pkind', vlu: val('dz-pkkind') }, { key: 'pallow', vlu: pk.allow }, { key: 'pterm', vlu: val('dz-pkq') }, { key: 'ptag', vlu: val('dz-pktag') }, { key: 'pctx', vlu: pk.tag }, { key: 'ppg', vlu: String(pk.page) }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(function () {
            pkList(1);
        }, 300);
    }

    function pkShown(html, more, tags, append) {
        var g = el('dz-pkgrid');
        if (append) {
            g.insertAdjacentHTML('beforeend', html);
        } else {
            g.innerHTML = html;
            g.scrollTop = 0;
        }
        el('dz-pkmore').innerHTML = more ? '<button type="button" class="dz-btn dz-btn-sm dz-btn-ghost" onclick="AdminDogsJs.pkList(' + (pk.page + 1) + ')">Show more</button>' : '';
        if (tags !== null) {
            var t = el('dz-pktag');
            var cur = t.value;
            t.innerHTML = '<option value="">Any tag</option>' + tags.map(function (x) {
                return '<option value="' + esc(x) + '">' + esc(x) + '</option>';
            }).join('');
            t.value = cur;
        }
        var b = g.querySelectorAll('.dz-pki');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('dz-on', pk.chosen.some(function (c) {
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
        b.classList.toggle('dz-on', at < 0);
        pkCount();
    }

    function pkCount() {
        var n = pk.chosen.length;
        el('dz-pkn').textContent = pk.multi ? (n ? n + ' selected' : 'Tap to select one or more') : 'Tap one to use it';
        el('dz-pkuse').disabled = n === 0;
        el('dz-pkuse').textContent = n > 1 ? 'Add ' + n : 'Add';
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
        var d = el('dz-pk');
        if (d) {
            d.classList.remove('dz-open');
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
        el('dz-pkerr').textContent = '';
        if (!f) {
            return;
        }
        if (!/^image\/(jpeg|png|webp)$/.test(f.type) || f.size > 30 * 1024 * 1024) {
            el('dz-pkerr').textContent = 'Choose a JPG, PNG or WebP photo under 30 MB.';
            input.value = '';
            return;
        }
        var url = URL.createObjectURL(f);
        var img = new Image();
        img.onload = function () {
            URL.revokeObjectURL(url);
            var full = pkShrink(img, 1600, 0.86);
            var thumb = pkShrink(img, 640, 0.8);
            el('dz-pkupl').classList.add('dz-busy');
            api('MediaUp', [{ key: 'full', vlu: full }, { key: 'thumb', vlu: thumb }, { key: 'pname', vlu: f.name.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ').substring(0, 80) }, { key: 'pctx', vlu: pk.tag }, { key: 'pw', vlu: String(img.naturalWidth) }, { key: 'ph', vlu: String(img.naturalHeight) }]);
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            el('dz-pkerr').textContent = 'That image could not be read.';
        };
        img.src = url;
        input.value = '';
    }

    function pkUploaded(ok, msg) {
        el('dz-pkupl').classList.remove('dz-busy');
        if (!ok) {
            el('dz-pkerr').textContent = msg;
            return;
        }
        el('dz-pkq').value = '';
        el('dz-pkkind').value = pk.allow === 'photo' ? 'photo' : '';
        el('dz-pktag').value = '';
        pkList(1);
        toast('Photo added to Media.');
    }

    var photos = [];
    var thumbs = {};
    var opened = '';

    window.AdminDogsJsloaded = function () {
        list(1);
        var id = qs('id');
        if (id) {
            edit(id === 'new' ? '' : id);
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: qs('status') + '|' + qs('sort') }];
    }

    function list(pg) {
        api('List', [{ key: 'dq', vlu: val('dz-q') }, { key: 'dstat', vlu: val('dz-fstat') }, { key: 'dsize', vlu: val('dz-fsize') }, { key: 'dsex', vlu: val('dz-fsex') }, { key: 'dsort', vlu: val('dz-fsort') }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function edit(id) {
        api('Form', [{ key: 'did', vlu: id || '' }]);
    }

    function pane(k) {
        var tabs = document.querySelectorAll('#dz-mbody .dz-tab');
        for (var i = 0; i < tabs.length; i++) {
            tabs[i].classList.toggle('dz-act', tabs[i].getAttribute('data-p') === k);
        }
        var ps = document.querySelectorAll('#dz-mbody .dz-panes > div');
        for (var j = 0; j < ps.length; j++) {
            ps[j].classList.toggle('dz-act', ps[j].getAttribute('data-p') === k);
        }
    }

    function opened2() {
        var d = el('dz-ddata');
        opened = d ? d.getAttribute('data-id') : '';
        try {
            photos = JSON.parse(d.getAttribute('data-photos') || '[]');
            thumbs = JSON.parse(d.getAttribute('data-thumbs') || '{}');
        } catch (e) {
            photos = [];
            thumbs = {};
        }
        drawPhotos();
        statView();
        openModal();
    }

    function statView() {
        var f = el('dz-dform');
        if (f) {
            f.classList.toggle('dz-isadopt', val('dz-dstatus') === 'adopted');
        }
    }

    function drawPhotos() {
        var box = el('dz-photos');
        if (!box) {
            return;
        }
        if (!photos.length) {
            box.innerHTML = '<p class="dz-none">No photos yet. Add some from Media.</p>';
        } else {
            var cover = -1;
            photos.forEach(function (p, i) {
                if (cover < 0 && !/\.(mp4|webm)$/i.test(p)) {
                    cover = i;
                }
            });
            box.innerHTML = photos.map(function (p, i) {
                var t = thumbs[p] || p.replace('/full/', '/thumbs/');
                var v = /\.(mp4|webm)$/i.test(p);
                return '<div class="dz-ph1">' + (i === cover ? '<span class="dz-pcov">Cover</span>' : '') + (v ? '<span class="dz-pvid">Video</span>' : '') + '<img src="' + esc(t) + '" alt="' + (v ? 'Video ' : 'Photo ') + (i + 1) + '"><div class="dz-pbtns"><button type="button" aria-label="Move earlier" onclick="AdminDogsJs.move(' + i + ',-1)"' + (i === 0 ? ' disabled' : '') + '><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 15l6-6 6 6"/></svg></button><button type="button" aria-label="Move later" onclick="AdminDogsJs.move(' + i + ',1)"' + (i === photos.length - 1 ? ' disabled' : '') + '><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 9l6 6 6-6"/></svg></button><button type="button" aria-label="Remove photo" onclick="AdminDogsJs.unpick(' + i + ')"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/></svg></button></div></div>';
            }).join('');
        }
        var n = el('dz-pcount');
        if (n) {
            n.textContent = String(photos.length);
        }
    }

    function move(i, d) {
        var j = i + d;
        if (j < 0 || j >= photos.length) {
            return;
        }
        var t = photos[i];
        photos[i] = photos[j];
        photos[j] = t;
        drawPhotos();
    }

    function unpick(i) {
        photos.splice(i, 1);
        drawPhotos();
    }

    function addMedia() {
        var nm = (val('dz-dname') || '').toLowerCase().replace(/[^a-z0-9 -]/g, '');
        pickMedia({ multi: true, tag: 'dogs' + (nm ? ',' + nm : ''), title: 'Add photos and videos', done: function (list) {
            var n = 0;
            list.forEach(function (it) {
                if (photos.indexOf(it.src) < 0 && photos.length < 12) {
                    photos.push(it.src);
                    thumbs[it.src] = it.thumb;
                    n++;
                }
            });
            drawPhotos();
            if (n < list.length) {
                toast(photos.length >= 12 ? 'A dog can have up to 12 photos and videos.' : 'Some were already on this dog.');
            }
        } });
    }

    function addHealth() {
        el('dz-health').insertAdjacentHTML('afterbegin', el('dz-htpl').innerHTML);
    }

    function dropRow(b) {
        b.parentNode.remove();
    }

    function save() {
        var health = [];
        var rows = document.querySelectorAll('#dz-health .dz-hrow2');
        for (var i = 0; i < rows.length; i++) {
            var x = rows[i].querySelectorAll('input, select');
            if (x[0].value && x[2].value.trim()) {
                health.push(x[0].value + '|' + x[1].value + '|' + x[2].value.trim().replace(/[|~]/g, ' '));
            }
        }
        var keys = ['dname', 'dbreed', 'dsex', 'dborn', 'dsize', 'dweight', 'dcolor', 'denergy', 'dkids', 'ddogs', 'dcats', 'dfee', 'dbonded', 'dneeds', 'dintake', 'dstatus', 'dadopter', 'dadopted', 'dstory', 'dtraits', 'dtail'];
        var list2 = keys.map(function (k) {
            return { key: k, vlu: el('dz-' + k) ? el('dz-' + k).value : '' };
        });
        ['dhouse', 'dvacc', 'dfixed', 'dchip', 'dfeat', 'durgent'].forEach(function (k) {
            list2.push({ key: k, vlu: el('dz-' + k) && el('dz-' + k).checked ? '1' : '0' });
        });
        list2.push({ key: 'did', vlu: opened });
        list2.push({ key: 'dphotos', vlu: photos.join('|') });
        list2.push({ key: 'dhealth', vlu: health.join('~') });
        api('Save', list2);
    }

    function saved(id, isNew) {
        closeModal();
        list(1);
        toast(isNew ? 'Dog ' + id + ' added.' : 'Saved.');
        try {
            history.replaceState(null, '', 'AdminDogs');
        } catch (e) {
        }
    }

    function fail(msg, p) {
        el('dz-e-save').textContent = msg;
        if (p) {
            pane(p);
        }
    }

    function remove(id, name) {
        ask({ title: 'Delete ' + name + '?', text: 'The dog disappears from the website and the admin. Applications, foster stays and donations keep their own records. This cannot be undone.', ok: 'Delete dog', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'did', vlu: id }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Dog deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('dz-', 'dz-e-') : '';
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
        edit: edit,
        pane: pane,
        opened: opened2,
        statView: statView,
        move: move,
        unpick: unpick,
        addMedia: addMedia,
        addHealth: addHealth,
        dropRow: dropRow,
        save: save,
        saved: saved,
        fail: fail,
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
    document.addEventListener('DOMContentLoaded', AdminDogsJs.reveal);
} else {
    AdminDogsJs.reveal();
}
