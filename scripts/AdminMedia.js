var AdminMediaJs = (function () {

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
        $ApiRequest('AdminMedia/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('md-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('md-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('md-open');
        }, 3200);
    }

    function openNav() {
        el('md-nav').classList.add('md-open');
        el('md-scrim').classList.add('md-open');
    }

    function closeNav() {
        el('md-nav').classList.remove('md-open');
        el('md-scrim').classList.remove('md-open');
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
        el('md-modal').classList.add('md-open');
        times(el('md-mbody'));
        var f = el('md-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('md-modal');
        if (m) {
            m.classList.remove('md-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('md-per')) {
            el('md-per').value = k;
        }
        if (k !== 'range') {
            if (el('md-from')) {
                el('md-from').value = '';
            }
            if (el('md-to')) {
                el('md-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('md-per') }, { key: 'pfrom', vlu: val('md-from') }, { key: 'pto', vlu: val('md-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('md-main'));
        if (window.MutationObserver && el('md-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('md-main'));
                }, 30);
            }).observe(el('md-main'), { childList: true, subtree: true });
        }
        if (window.AdminMediaJsloaded) {
            window.AdminMediaJsloaded();
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
        if (!askBox || !askBox.classList.contains('md-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('md-open');
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
        askBox.className = 'md-ask';
        askBox.innerHTML = '<div class="md-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="md-err" role="alert"></em><div class="md-askacts"><button type="button" class="md-btn md-btn-ghost md-btn-sm"></button><button type="button" class="md-btn md-btn-sm"></button></div></div>';
        (el('md-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('md-open')) {
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
        x.b[1].className = 'md-btn md-btn-sm' + (o.danger ? ' md-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('md-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('md-open');
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

    var kind = '';
    var page = 1;
    var full = '';
    var thumb = '';
    var pw = 0;
    var ph = 0;
    var vfile = null;
    var vposter = '';
    var vsecs = 0;
    var vw = 0;
    var vh = 0;
    var vuid = '';
    var vtotal = 0;
    var current = '';
    var CHUNK = 1024 * 1024;

    window.AdminMediaJsloaded = function () {
        list(1);
        if (qs('id')) {
            view(qs('id'));
        }
    };

    function loadArgs() {
        return [];
    }

    function filters() {
        return [{ key: 'mkind', vlu: kind }, { key: 'mq', vlu: val('md-q') }, { key: 'mtag', vlu: val('md-mtag') }, { key: 'muse', vlu: val('md-muse') }];
    }

    function list(pg) {
        page = pg || 1;
        api('List', filters().concat([{ key: 'pg', vlu: String(page) }]));
    }

    function setKind(k) {
        kind = k;
        list(1);
    }

    function shrink(img, max, type, q) {
        var r = Math.min(1, max / Math.max(img.naturalWidth, img.naturalHeight));
        var c = document.createElement('canvas');
        c.width = Math.round(img.naturalWidth * r);
        c.height = Math.round(img.naturalHeight * r);
        c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
        var d = c.toDataURL(type, q);
        return d.indexOf('data:' + type) === 0 ? d : c.toDataURL('image/jpeg', q);
    }

    function mb(n) {
        return (n / 1048576).toFixed(n < 10485760 ? 1 : 0) + ' MB';
    }

    function clock(sec) {
        return Math.floor(sec / 60) + ':' + (sec % 60 < 10 ? '0' : '') + (sec % 60);
    }

    function setBox(boxId, inputId, html) {
        var inp = el(inputId);
        var box = el(boxId);
        box.innerHTML = html;
        box.appendChild(inp);
    }

    function nameOf(f) {
        return f.name.replace(/\.[^.]+$/, '').replace(/[-_]+/g, ' ').substring(0, 80);
    }

    function file(input) {
        var f = input.files && input.files[0];
        el('md-e-file').textContent = '';
        full = '';
        thumb = '';
        if (!f) {
            return;
        }
        if (!/^image\/(jpeg|png|webp)$/.test(f.type) || f.size > 30 * 1024 * 1024) {
            el('md-e-file').textContent = 'Use a JPG, PNG or WebP photo under 30 MB.';
            return;
        }
        if (!val('md-ptitle')) {
            el('md-ptitle').value = nameOf(f);
        }
        var label = esc(f.name) + ' &middot; ' + mb(f.size);
        setBox('md-prev', 'md-file', '<span>Reading&hellip;</span><span class="md-finfo">' + label + '</span>');
        var url = URL.createObjectURL(f);
        var img = new Image();
        img.onload = function () {
            URL.revokeObjectURL(url);
            full = shrink(img, 1600, 'image/webp', 0.86);
            thumb = shrink(img, 640, 'image/webp', 0.8);
            if (full.substring(0, 15) !== thumb.substring(0, 15)) {
                full = shrink(img, 1600, 'image/jpeg', 0.86);
                thumb = shrink(img, 640, 'image/jpeg', 0.8);
            }
            pw = img.naturalWidth;
            ph = img.naturalHeight;
            setBox('md-prev', 'md-file', '<img src="' + thumb + '" alt=""><span class="md-finfo">' + label + ' &middot; ' + pw + '&times;' + ph + '</span>');
        };
        img.onerror = function () {
            URL.revokeObjectURL(url);
            setBox('md-prev', 'md-file', '<span>Choose a photo<br>JPG, PNG or WebP</span>');
            el('md-e-file').textContent = 'That image could not be read.';
        };
        img.src = url;
    }

    function upload() {
        if (!full) {
            el('md-e-file').textContent = 'Choose a photo first.';
            return;
        }
        if (val('md-ptitle').length < 2) {
            el('md-e-file').textContent = 'Give the photo a name.';
            return;
        }
        el('md-pbtn').disabled = true;
        api('Upload', [{ key: 'full', vlu: full }, { key: 'thumb', vlu: thumb }, { key: 'mtitle', vlu: val('md-ptitle') }, { key: 'malt', vlu: val('md-palt') }, { key: 'mtags', vlu: val('md-ptags') }, { key: 'mw', vlu: String(pw) }, { key: 'mh', vlu: String(ph) }]);
    }

    function upFail(msg) {
        el('md-e-file').textContent = msg;
        el('md-pbtn').disabled = false;
    }

    function uploaded() {
        full = '';
        thumb = '';
        el('md-file').value = '';
        setBox('md-prev', 'md-file', '<span>Choose a photo<br>JPG, PNG or WebP</span>');
        ['md-ptitle', 'md-palt', 'md-ptags'].forEach(function (k) {
            el(k).value = '';
        });
        el('md-pbtn').disabled = false;
        kind = '';
        list(1);
        toast('Photo added to Media.');
    }

    function capture(f, src, label) {
        var v = document.createElement('video');
        var finished = false;
        var stop = function () {
            if (!finished) {
                finished = true;
                URL.revokeObjectURL(src);
            }
        };
        v.muted = true;
        v.preload = 'auto';
        v.playsInline = true;
        v.addEventListener('loadedmetadata', function () {
            vsecs = isFinite(v.duration) ? Math.round(v.duration) : 0;
            vw = v.videoWidth;
            vh = v.videoHeight;
            v.currentTime = Math.min(1, (v.duration || 2) / 2);
        });
        v.addEventListener('seeked', function () {
            try {
                var r = Math.min(1, 960 / v.videoWidth);
                var c = document.createElement('canvas');
                c.width = Math.round(v.videoWidth * r);
                c.height = Math.round(v.videoHeight * r);
                c.getContext('2d').drawImage(v, 0, 0, c.width, c.height);
                vposter = c.toDataURL('image/jpeg', 0.82);
                setBox('md-vprev', 'md-vfile', '<img src="' + vposter + '" alt=""><span class="md-play"></span><span class="md-finfo">' + label + (vsecs ? ' &middot; ' + clock(vsecs) : '') + '</span>');
            } catch (e) {
                vposter = '';
            }
            stop();
        });
        v.addEventListener('error', function () {
            setBox('md-vprev', 'md-vfile', '<span>' + esc(f.name) + '<br>No preview in this browser</span><span class="md-finfo">' + label + '</span>');
            stop();
        });
        v.src = src;
    }

    function vpick(input) {
        var f = input.files && input.files[0];
        var max = parseInt(input.getAttribute('data-max'), 10);
        el('md-e-vfile').textContent = '';
        vfile = null;
        vposter = '';
        vsecs = 0;
        if (!f) {
            return;
        }
        if (!/^video\/(mp4|webm)$/.test(f.type) && !/\.(mp4|webm)$/i.test(f.name)) {
            el('md-e-vfile').textContent = 'Use an MP4 or WebM video.';
            return;
        }
        if (f.size > max) {
            el('md-e-vfile').textContent = 'The video must be under ' + Math.round(max / 1048576) + ' MB.';
            return;
        }
        vfile = f;
        if (!val('md-vtitle')) {
            el('md-vtitle').value = nameOf(f);
        }
        var label = esc(f.name) + ' &middot; ' + mb(f.size);
        setBox('md-vprev', 'md-vfile', '<span>Reading&hellip;</span><span class="md-finfo">' + label + '</span>');
        capture(f, URL.createObjectURL(f), label);
    }

    function hex() {
        var a = new Uint8Array(16);
        window.crypto.getRandomValues(a);
        var s = '';
        for (var i = 0; i < a.length; i++) {
            s += (a[i] < 16 ? '0' : '') + a[i].toString(16);
        }
        return s;
    }

    function vupload() {
        if (!vfile) {
            el('md-e-vfile').textContent = 'Choose a video first.';
            return;
        }
        if (val('md-vtitle').length < 2) {
            el('md-e-vfile').textContent = 'Give the video a name.';
            return;
        }
        if (vfile.size > 20 * 1024 * 1024) {
            ask({ title: 'This video is ' + mb(vfile.size) + '.', text: 'Large videos load slowly for visitors on phones. A short clip under 20 MB works best. Upload it anyway?', ok: 'Upload anyway', no: 'Choose another' }, go);
            return;
        }
        go();
    }

    function go() {
        vuid = hex();
        vtotal = Math.ceil(vfile.size / CHUNK);
        el('md-vbtn').disabled = true;
        el('md-vprog').classList.add('md-on');
        send(0);
    }

    function send(i) {
        var r = new FileReader();
        r.onload = function () {
            var d = String(r.result);
            api('Chunk', [{ key: 'uid', vlu: vuid }, { key: 'idx', vlu: String(i) }, { key: 'total', vlu: String(vtotal) }, { key: 'part', vlu: d.substring(d.indexOf(',') + 1) }]);
        };
        r.onerror = function () {
            chunkFail('That file could not be read.');
        };
        r.readAsDataURL(vfile.slice(i * CHUNK, (i + 1) * CHUNK));
    }

    function chunkOk(i) {
        el('md-vbar').style.width = Math.round((i + 1) / vtotal * 100) + '%';
        if (i + 1 < vtotal) {
            send(i + 1);
            return;
        }
        api('VideoDone', [{ key: 'uid', vlu: vuid }, { key: 'total', vlu: String(vtotal) }, { key: 'mtitle', vlu: val('md-vtitle') }, { key: 'mtags', vlu: val('md-vtags') }, { key: 'poster', vlu: vposter }, { key: 'secs', vlu: String(vsecs) }, { key: 'mw', vlu: String(vw) }, { key: 'mh', vlu: String(vh) }]);
    }

    function chunkFail(msg) {
        el('md-e-vfile').textContent = msg;
        el('md-vbtn').disabled = false;
        el('md-vprog').classList.remove('md-on');
        el('md-vbar').style.width = '0';
    }

    function vdone() {
        vfile = null;
        vposter = '';
        el('md-vfile').value = '';
        setBox('md-vprev', 'md-vfile', '<span>Choose a video<br>MP4 or WebM</span>');
        el('md-vtitle').value = '';
        el('md-vtags').value = '';
        chunkFail('');
        kind = '';
        list(1);
        toast('Video added to Media.');
    }

    function view(id) {
        current = id;
        api('Open', [{ key: 'mid', vlu: id }]);
    }

    function stopPreview() {
        var v = document.querySelector('#md-mbody video');
        if (v) {
            v.pause();
        }
    }

    function save() {
        api('SaveInfo', [{ key: 'mid', vlu: current }, { key: 'mtitle', vlu: val('md-etitle') }, { key: 'malt', vlu: val('md-ealt') }, { key: 'mtags', vlu: val('md-etags') }].concat(filters(), [{ key: 'pg', vlu: String(page) }]));
    }

    function saved() {
        stopPreview();
        closeModal();
        toast('Saved.');
    }

    function del(id, title) {
        ask({ title: 'Delete “' + title + '”?', text: 'It is removed from the library for good.', ok: 'Delete', no: 'Keep it', danger: true }, function () {
            stopPreview();
            closeModal();
            api('Remove', [{ key: 'mid', vlu: id }, { key: 'pg', vlu: String(page) }].concat(filters()));
        });
    }

    document.addEventListener('click', function (e) {
        if (e.target && (e.target.id === 'md-modal' || (e.target.closest && e.target.closest('.md-mclose')))) {
            stopPreview();
        }
    });

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
            var id = e.target && e.target.id ? e.target.id.replace('md-', 'md-e-') : '';
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
        setKind: setKind,
        file: file,
        upload: upload,
        upFail: upFail,
        uploaded: uploaded,
        vpick: vpick,
        vupload: vupload,
        chunkOk: chunkOk,
        chunkFail: chunkFail,
        vdone: vdone,
        view: view,
        stopPreview: stopPreview,
        save: save,
        saved: saved,
        del: del
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminMediaJs.reveal);
} else {
    AdminMediaJs.reveal();
}
