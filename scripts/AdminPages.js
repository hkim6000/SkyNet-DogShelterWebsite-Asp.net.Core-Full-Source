var AdminPagesJs = (function () {

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
        $ApiRequest('AdminPages/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('pg-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('pg-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('pg-open');
        }, 3200);
    }

    function openNav() {
        el('pg-nav').classList.add('pg-open');
        el('pg-scrim').classList.add('pg-open');
    }

    function closeNav() {
        el('pg-nav').classList.remove('pg-open');
        el('pg-scrim').classList.remove('pg-open');
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
        el('pg-modal').classList.add('pg-open');
        times(el('pg-mbody'));
        var f = el('pg-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('pg-modal');
        if (m) {
            m.classList.remove('pg-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('pg-per')) {
            el('pg-per').value = k;
        }
        if (k !== 'range') {
            if (el('pg-from')) {
                el('pg-from').value = '';
            }
            if (el('pg-to')) {
                el('pg-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('pg-per') }, { key: 'pfrom', vlu: val('pg-from') }, { key: 'pto', vlu: val('pg-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('pg-main'));
        if (window.MutationObserver && el('pg-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('pg-main'));
                }, 30);
            }).observe(el('pg-main'), { childList: true, subtree: true });
        }
        if (window.AdminPagesJsloaded) {
            window.AdminPagesJsloaded();
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
        if (!askBox || !askBox.classList.contains('pg-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('pg-open');
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
        askBox.className = 'pg-ask';
        askBox.innerHTML = '<div class="pg-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="pg-err" role="alert"></em><div class="pg-askacts"><button type="button" class="pg-btn pg-btn-ghost pg-btn-sm"></button><button type="button" class="pg-btn pg-btn-sm"></button></div></div>';
        (el('pg-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('pg-open')) {
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
        x.b[1].className = 'pg-btn pg-btn-sm' + (o.danger ? ' pg-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('pg-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('pg-open');
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
        if (el('pg-pk')) {
            return;
        }
        var d = document.createElement('div');
        d.className = 'pg-pk';
        d.id = 'pg-pk';
        d.setAttribute('role', 'dialog');
        d.setAttribute('aria-modal', 'true');
        d.innerHTML = '<div class="pg-pkbox"><header><h2 id="pg-pkt">Choose from Media</h2><button type="button" class="pg-ibtn" aria-label="Close" onclick="AdminPagesJs.pkClose()">&times;</button></header><div class="pg-pkbar"><input type="search" id="pg-pkq" placeholder="Search by name or tag" maxlength="60" oninput="AdminPagesJs.pkSearch()"><select id="pg-pkkind" aria-label="Type" onchange="AdminPagesJs.pkList(1)"><option value="">Photos and videos</option><option value="photo">Photos</option><option value="video">Videos</option></select><select id="pg-pktag" aria-label="Tag" onchange="AdminPagesJs.pkList(1)"><option value="">Any tag</option></select><label class="pg-btn pg-btn-sm pg-btn-ghost pg-pkup" id="pg-pkupl">Upload a photo<input type="file" id="pg-pkfile" accept="image/jpeg,image/png,image/webp" onchange="AdminPagesJs.pkUpload(this)"></label></div><em class="pg-err pg-pkerr" id="pg-pkerr" role="alert"></em><div class="pg-pkgrid" id="pg-pkgrid"></div><div class="pg-pkmore" id="pg-pkmore"></div><footer><span id="pg-pkn"></span><button type="button" class="pg-btn pg-btn-ghost" onclick="AdminPagesJs.pkClose()">Cancel</button><button type="button" class="pg-btn pg-btn-accent" id="pg-pkuse" onclick="AdminPagesJs.pkUse()">Add</button></footer></div>';
        (el('pg-site') || document.body).appendChild(d);
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
        el('pg-pkt').textContent = opts.title || 'Choose from Media';
        el('pg-pkq').value = '';
        el('pg-pkerr').textContent = '';
        var k = el('pg-pkkind');
        k.value = pk.allow === 'photo' ? 'photo' : '';
        k.disabled = pk.allow === 'photo' || pk.allow === 'video';
        if (pk.allow === 'video') {
            k.value = 'video';
        }
        el('pg-pkupl').hidden = pk.allow === 'video';
        el('pg-pkuse').hidden = !pk.multi;
        pkCount();
        el('pg-pk').classList.add('pg-open');
        pkList(1);
        setTimeout(function () {
            el('pg-pkq').focus();
        }, 30);
    }

    function pkList(pg) {
        pk.page = pg || 1;
        api('MediaPick', [{ key: 'pkind', vlu: val('pg-pkkind') }, { key: 'pallow', vlu: pk.allow }, { key: 'pterm', vlu: val('pg-pkq') }, { key: 'ptag', vlu: val('pg-pktag') }, { key: 'pctx', vlu: pk.tag }, { key: 'ppg', vlu: String(pk.page) }]);
    }

    function pkSearch() {
        clearTimeout(pkTimer);
        pkTimer = setTimeout(function () {
            pkList(1);
        }, 300);
    }

    function pkShown(html, more, tags, append) {
        var g = el('pg-pkgrid');
        if (append) {
            g.insertAdjacentHTML('beforeend', html);
        } else {
            g.innerHTML = html;
            g.scrollTop = 0;
        }
        el('pg-pkmore').innerHTML = more ? '<button type="button" class="pg-btn pg-btn-sm pg-btn-ghost" onclick="AdminPagesJs.pkList(' + (pk.page + 1) + ')">Show more</button>' : '';
        if (tags !== null) {
            var t = el('pg-pktag');
            var cur = t.value;
            t.innerHTML = '<option value="">Any tag</option>' + tags.map(function (x) {
                return '<option value="' + esc(x) + '">' + esc(x) + '</option>';
            }).join('');
            t.value = cur;
        }
        var b = g.querySelectorAll('.pg-pki');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('pg-on', pk.chosen.some(function (c) {
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
        b.classList.toggle('pg-on', at < 0);
        pkCount();
    }

    function pkCount() {
        var n = pk.chosen.length;
        el('pg-pkn').textContent = pk.multi ? (n ? n + ' selected' : 'Tap to select one or more') : 'Tap one to use it';
        el('pg-pkuse').disabled = n === 0;
        el('pg-pkuse').textContent = n > 1 ? 'Add ' + n : 'Add';
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
        var d = el('pg-pk');
        if (d) {
            d.classList.remove('pg-open');
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
        el('pg-pkerr').textContent = '';
        if (!f) {
            return;
        }
        if (!/^image\/(jpeg|png|webp)$/.test(f.type) || f.size > 30 * 1024 * 1024) {
            el('pg-pkerr').textContent = 'Choose a JPG, PNG or WebP photo under 30 MB.';
            input.value = '';
            return;
        }
        var url = URL.createObjectURL(f);
        var img = new Image();
        img.onload = function () {
            URL.revokeObjectURL(url);
            var full = pkShrink(img, 1600, 0.86);
            var thumb = pkShrink(img, 640, 0.8);
            el('pg-pkupl').classList.add('pg-busy');
            api('MediaUp', [{ key: 'full', vlu: full }, { key: 'thumb', vlu: thumb }, { key: 'pname', vlu: f.name.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ').substring(0, 80) }, { key: 'pctx', vlu: pk.tag }, { key: 'pw', vlu: String(img.naturalWidth) }, { key: 'ph', vlu: String(img.naturalHeight) }]);
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            el('pg-pkerr').textContent = 'That image could not be read.';
        };
        img.src = url;
        input.value = '';
    }

    function pkUploaded(ok, msg) {
        el('pg-pkupl').classList.remove('pg-busy');
        if (!ok) {
            el('pg-pkerr').textContent = msg;
            return;
        }
        el('pg-pkq').value = '';
        el('pg-pkkind').value = pk.allow === 'photo' ? 'photo' : '';
        el('pg-pktag').value = '';
        pkList(1);
        toast('Photo added to Media.');
    }

    var page = 'home';
    var dirty = false;
    var drag = null;
    var startOrder = '';
    var mediaFor = null;
    var richAt = null;
    var range = null;
    var rtTimer = null;

    window.AdminPagesJsloaded = function () {
        show(qs('page') || 'home');
        window.addEventListener('beforeunload', function (e) {
            if (dirty) {
                e.preventDefault();
                e.returnValue = '';
            }
        });
    };

    function loadArgs() {
        return [];
    }

    function show(p) {
        api('Show', [{ key: 'page', vlu: p }]);
    }

    function tab(p) {
        if (dirty) {
            ask({ title: 'Leave without saving?', text: 'Your changes to this page will be lost.', ok: 'Leave', no: 'Stay', danger: true }, function () {
                dirty = false;
                show(p);
            });
            return;
        }
        show(p);
    }

    function mark() {
        var d = el('pg-dirty');
        if (d) {
            d.textContent = dirty ? 'Unsaved changes' : '';
        }
    }

    function touch() {
        dirty = true;
        mark();
    }

    function shown(p) {
        page = p;
        dirty = false;
        mark();
        try {
            history.replaceState(null, '', 'AdminPages?page=' + p);
        } catch (e) {
        }
        var c = el('pg-canvas');
        if (!c || c.getAttribute('data-bound')) {
            return;
        }
        c.setAttribute('data-bound', '1');
        c.addEventListener('input', touch);
        c.addEventListener('change', touch);
        c.addEventListener('paste', function (e) {
            var t = e.target.closest ? e.target.closest('[contenteditable]') : null;
            if (t) {
                e.preventDefault();
                var txt = (e.clipboardData || window.clipboardData).getData('text/plain');
                document.execCommand('insertText', false, t.getAttribute('data-ml') === '1' || t.classList.contains('pg-er') ? txt : txt.replace(/\s*\n\s*/g, ' '));
            }
        });
        c.addEventListener('keydown', function (e) {
            var t = e.target;
            if (e.key === 'Enter' && t.getAttribute && t.getAttribute('contenteditable') === 'plaintext-only' && t.getAttribute('data-ml') !== '1') {
                e.preventDefault();
            }
        });
        c.addEventListener('focusin', function (e) {
            if (e.target.classList && e.target.classList.contains('pg-er')) {
                richAt = e.target;
                clearTimeout(rtTimer);
                tools(true);
            }
        });
        window.addEventListener('scroll', function () {
            if (richAt && el('pg-rt').classList.contains('pg-open')) {
                tools(true);
            }
        }, { passive: true });
        c.addEventListener('focusout', function (e) {
            if (e.target.classList && e.target.classList.contains('pg-er')) {
                rtTimer = setTimeout(function () {
                    tools(false);
                }, 180);
            }
        });
    }

    function tools(on) {
        var t = el('pg-rt');
        if (!t) {
            return;
        }
        if (!on || !richAt) {
            t.classList.remove('pg-open');
            return;
        }
        var r = richAt.getBoundingClientRect();
        t.style.left = Math.max(8, Math.min(r.left, window.innerWidth - 260)) + 'px';
        t.style.top = Math.max(64, r.top - 42) + 'px';
        t.classList.add('pg-open');
    }

    function fmt(cmd) {
        if (!richAt) {
            return;
        }
        richAt.focus();
        if (cmd === 'link') {
            var sel = window.getSelection();
            range = sel.rangeCount ? sel.getRangeAt(0).cloneRange() : null;
            if (!range || range.collapsed) {
                toast('Select some words first, then add the link.');
                return;
            }
            var target = richAt;
            ask({ title: 'Add a link', text: 'A page name like Dogs or Adopt#apply, or a full address starting with https://', value: 'https://', ok: 'Add link', check: function (v) {
                return /^(https?:\/\/\S+|mailto:\S+|tel:[0-9+() .-]+|#[A-Za-z0-9_-]+|[A-Za-z][A-Za-z0-9_-]*(\?[A-Za-z0-9=&%._+-]*)?(#[A-Za-z0-9_-]+)?)$/.test(v) ? '' : 'Use a page name or a full https:// address.';
            } }, function (v) {
                target.focus();
                var s2 = window.getSelection();
                s2.removeAllRanges();
                s2.addRange(range);
                document.execCommand('createLink', false, v);
                touch();
            });
            return;
        }
        if (cmd === 'clear') {
            document.execCommand('removeFormat');
            document.execCommand('unlink');
        } else {
            document.execCommand(cmd);
        }
        touch();
    }

    function blocks() {
        return el('pg-canvas').querySelectorAll(':scope > .pg-blk');
    }

    function order() {
        return [].map.call(blocks(), function (b) {
            return b.getAttribute('data-id');
        }).join(',');
    }

    function grab(e, btn) {
        e.preventDefault();
        drag = btn.closest('.pg-blk');
        startOrder = order();
        drag.classList.add('pg-dragging');
        document.addEventListener('pointermove', dragMove);
        document.addEventListener('pointerup', dragEnd);
        document.addEventListener('pointercancel', dragEnd);
    }

    function dragMove(ev) {
        if (!drag) {
            return;
        }
        ev.preventDefault();
        var t = document.elementFromPoint(ev.clientX, ev.clientY);
        var over = t && t.closest ? t.closest('#pg-canvas > .pg-blk') : null;
        if (over && over !== drag) {
            var r = over.getBoundingClientRect();
            over.parentNode.insertBefore(drag, ev.clientY > r.top + r.height / 2 ? over.nextSibling : over);
        }
        if (ev.clientY < 120) {
            window.scrollBy(0, -16);
        } else if (ev.clientY > window.innerHeight - 60) {
            window.scrollBy(0, 16);
        }
    }

    function dragEnd() {
        document.removeEventListener('pointermove', dragMove);
        document.removeEventListener('pointerup', dragEnd);
        document.removeEventListener('pointercancel', dragEnd);
        if (!drag) {
            return;
        }
        drag.classList.remove('pg-dragging');
        drag = null;
        if (order() !== startOrder) {
            touch();
        }
    }

    function move(btn, dir) {
        var b = btn.closest('.pg-blk');
        var sib = dir < 0 ? b.previousElementSibling : b.nextElementSibling;
        if (!sib || !sib.classList.contains('pg-blk')) {
            return;
        }
        if (dir < 0) {
            b.parentNode.insertBefore(b, sib);
        } else {
            b.parentNode.insertBefore(sib, b);
        }
        b.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        touch();
    }

    function hide(btn) {
        var b = btn.closest('.pg-blk');
        var on = b.getAttribute('data-hidden') !== '1';
        b.setAttribute('data-hidden', on ? '1' : '0');
        b.classList.toggle('pg-hid', on);
        btn.querySelector('span').textContent = on ? 'Show' : 'Hide';
        btn.setAttribute('aria-pressed', on ? 'true' : 'false');
        touch();
    }

    function drop(btn) {
        var b = btn.closest('.pg-blk');
        ask({ title: 'Remove this block?', text: 'It disappears from the page when you save. Use Hide to keep it for later.', ok: 'Remove', no: 'Keep it', danger: true }, function () {
            b.parentNode.removeChild(b);
            touch();
        });
    }

    function media(btn) {
        mediaFor = btn.closest('.pg-em');
        var allow = mediaFor.getAttribute('data-allow') || '';
        pickMedia({ allow: allow, tag: 'pages', title: allow === 'photo' ? 'Choose a photo' : 'Choose a photo or video', done: function (list) {
            var it = list[0];
            mediaFor.setAttribute('data-src', it.src);
            paintMedia(mediaFor, it.thumb, it.kind === 'video');
            touch();
        } });
    }

    function noMedia(btn) {
        var m = btn.closest('.pg-em');
        m.setAttribute('data-src', '');
        paintMedia(m, '', false);
        touch();
    }

    function paintMedia(m, thumb, vid) {
        var b = m.querySelector('.pg-emb');
        b.innerHTML = thumb ? '<img src="' + esc(thumb) + '" alt="">' + (vid ? '<span class="pg-emv">Video</span>' : '') + '<span class="pg-emc">Change</span>' : '<span>' + esc(m.getAttribute('data-ph') || 'Add a picture') + '</span>';
        var x = m.querySelector('.pg-emx');
        if (x) {
            x.hidden = !thumb;
        }
    }

    function addBlock(type) {
        api('AddBlock', [{ key: 'btype', vlu: type }]);
    }

    function added() {
        var h = el('pg-holder');
        var b = h.firstElementChild;
        if (!b) {
            return;
        }
        el('pg-canvas').insertBefore(b, el('pg-addp'));
        b.scrollIntoView({ block: 'center', behavior: 'smooth' });
        var f = b.querySelector('[contenteditable]');
        if (f) {
            setTimeout(function () {
                f.focus();
            }, 300);
        }
        touch();
    }

    function addItem(btn) {
        var box = btn.parentNode.querySelector('.pg-eitems');
        var tpl = btn.parentNode.querySelector('template');
        if (box.children.length >= 12) {
            toast('A block can have up to 12 items.');
            return;
        }
        box.insertAdjacentHTML('beforeend', tpl.innerHTML);
        var f = box.lastElementChild.querySelector('[contenteditable]');
        if (f) {
            f.focus();
        }
        touch();
    }

    function itemMove(btn, dir) {
        var it = btn.closest('.pg-eit');
        var sib = dir < 0 ? it.previousElementSibling : it.nextElementSibling;
        if (!sib) {
            return;
        }
        if (dir < 0) {
            it.parentNode.insertBefore(it, sib);
        } else {
            it.parentNode.insertBefore(sib, it);
        }
        touch();
    }

    function itemDrop(btn) {
        var it = btn.closest('.pg-eit');
        it.parentNode.removeChild(it);
        touch();
    }

    function cleanNode(n) {
        var out = '';
        [].forEach.call(n.childNodes, function (c) {
            if (c.nodeType === 3) {
                out += esc(c.nodeValue);
                return;
            }
            if (c.nodeType !== 1) {
                return;
            }
            var t = c.tagName.toLowerCase();
            if (/^(script|style|template|iframe|object|img|video|svg)$/.test(t)) {
                return;
            }
            var inner = cleanNode(c);
            if (t === 'br') {
                out += '<br>';
            } else if (t === 'b' || t === 'strong') {
                out += inner ? '<b>' + inner + '</b>' : '';
            } else if (t === 'i' || t === 'em') {
                out += inner ? '<i>' + inner + '</i>' : '';
            } else if (t === 'a') {
                var h = c.getAttribute('href') || '';
                out += h ? '<a href="' + esc(h) + '">' + inner + '</a>' : inner;
            } else if (t === 'ul' || t === 'ol' || t === 'li') {
                out += '<' + t + '>' + inner + '</' + t + '>';
            } else if (t === 'p' || t === 'div' || /^h[1-6]$/.test(t)) {
                out += '<p>' + inner + '</p>';
            } else {
                out += inner;
            }
        });
        return out;
    }

    function textOf(f) {
        if (f.classList.contains('pg-er')) {
            return cleanNode(f);
        }
        return (f.innerText || '').replace(/ /g, ' ').replace(/\n{3,}/g, '\n\n').trim();
    }

    function collect() {
        return [].map.call(blocks(), function (b) {
            var o = { id: b.getAttribute('data-id'), type: b.getAttribute('data-type'), source: b.getAttribute('data-src') || '', hidden: b.getAttribute('data-hidden') === '1', kicker: '', title: '', text: '', media: '', label: '', link: '', label2: '', link2: '', flip: false, extra: '', items: [] };
            [].forEach.call(b.querySelectorAll('[data-f]'), function (f) {
                var k = f.getAttribute('data-f');
                if (k === 'media') {
                    o.media = f.getAttribute('data-src') || '';
                } else if (k === 'flip') {
                    o.flip = f.checked;
                } else if (k === 'items') {
                    o.items = [].map.call(f.querySelectorAll(':scope > .pg-eit'), function (it) {
                        var r = { icon: '', title: '', text: '', link: '', label: '' };
                        [].forEach.call(it.querySelectorAll('[data-i]'), function (x) {
                            var n = x.getAttribute('data-i');
                            r[n] = x.tagName === 'INPUT' || x.tagName === 'SELECT' ? x.value.trim() : textOf(x);
                        });
                        return r;
                    });
                } else if (f.tagName === 'INPUT' || f.tagName === 'SELECT') {
                    o[k] = f.value.trim();
                } else {
                    o[k] = textOf(f);
                }
            });
            return o;
        });
    }

    function save() {
        tools(false);
        el('pg-savebtn').disabled = true;
        api('Save', [{ key: 'page', vlu: page }, { key: 'blocks', vlu: JSON.stringify(collect()) }]);
    }

    function saved(msg) {
        el('pg-savebtn').disabled = false;
        dirty = false;
        mark();
        toast(msg);
    }

    function fail(msg) {
        el('pg-savebtn').disabled = false;
        toast(msg);
    }

    function versions() {
        api('Versions', [{ key: 'page', vlu: page }]);
    }

    function restore(i, when) {
        ask({ title: 'Restore the version from ' + when + '?', text: 'The page goes back to that version right away. The current version is kept in the list, so you can switch back.', ok: 'Restore', no: 'Cancel' }, function () {
            closeModal();
            api('Restore', [{ key: 'page', vlu: page }, { key: 'vi', vlu: String(i) }]);
        });
    }

    function view() {
        var a = el('pg-viewlink');
        if (dirty) {
            toast('Save first to see your changes on the website.');
        }
        window.open(a.getAttribute('data-href'), '_blank', 'noopener');
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
            var id = e.target && e.target.id ? e.target.id.replace('pg-', 'pg-e-') : '';
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
        tab: tab,
        shown: shown,
        fmt: fmt,
        grab: grab,
        move: move,
        hide: hide,
        drop: drop,
        media: media,
        noMedia: noMedia,
        addBlock: addBlock,
        added: added,
        addItem: addItem,
        itemMove: itemMove,
        itemDrop: itemDrop,
        save: save,
        saved: saved,
        fail: fail,
        versions: versions,
        restore: restore,
        view: view,
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
    document.addEventListener('DOMContentLoaded', AdminPagesJs.reveal);
} else {
    AdminPagesJs.reveal();
}
