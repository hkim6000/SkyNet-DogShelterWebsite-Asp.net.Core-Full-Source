var DogJs = (function () {

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
        $ApiRequest('Dog/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('dd-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('dd-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('dd-open');
        }, 3200);
    }

    function openNav() {
        el('dd-nav').classList.add('dd-open');
        el('dd-scrim').classList.add('dd-open');
    }

    function closeNav() {
        el('dd-nav').classList.remove('dd-open');
        el('dd-scrim').classList.remove('dd-open');
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

    var askBox = null;
    var askDone = null;
    var askCheck = null;
    var askBack = null;

    function askParts() {
        return { h: askBox.querySelector('h2'), p: askBox.querySelector('p'), i: askBox.querySelector('input'), e: askBox.querySelector('em'), b: askBox.querySelectorAll('button') };
    }

    function askClose(ok) {
        if (!askBox || !askBox.classList.contains('dd-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('dd-open');
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
        askBox.className = 'dd-ask';
        askBox.innerHTML = '<div class="dd-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="dd-err" role="alert"></em><div class="dd-askacts"><button type="button" class="dd-btn dd-btn-ghost dd-btn-sm"></button><button type="button" class="dd-btn dd-btn-sm"></button></div></div>';
        (el('dd-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('dd-open')) {
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
        x.b[1].className = 'dd-btn dd-btn-sm' + (o.danger ? ' dd-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('dd-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('dd-open');
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

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function applyTheme(theme) {
        el('dd-site').setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
    }

    function toggleTheme() {
        var next = el('dd-site').getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        applyTheme(next);
        try {
            localStorage.setItem('wd-theme', next);
        } catch (e) {
        }
    }

    function savedList() {
        try {
            var v = JSON.parse(localStorage.getItem('wd-saved') || '[]');
            return Array.isArray(v) ? v.filter(function (x) {
                return typeof x === 'string' && /^[A-Za-z0-9-]{1,20}$/.test(x);
            }) : [];
        } catch (e) {
            return [];
        }
    }

    function savedStore(list) {
        try {
            localStorage.setItem('wd-saved', JSON.stringify(list.slice(0, 200)));
            return true;
        } catch (e) {
            return false;
        }
    }

    function savedCount() {
        var n = savedList().length;
        var c = el('dd-scount');
        if (c) {
            c.textContent = n > 99 ? '99+' : String(n);
            c.hidden = n === 0;
        }
    }

    function hearts(root) {
        var s = savedList();
        var list = (root || document).querySelectorAll('[data-heart]');
        for (var i = 0; i < list.length; i++) {
            var on = s.indexOf(list[i].getAttribute('data-heart')) >= 0;
            list[i].setAttribute('aria-pressed', on ? 'true' : 'false');
            list[i].setAttribute('aria-label', on ? 'Remove from saved dogs' : 'Save this dog');
            list[i].title = on ? 'Saved' : 'Save';
        }
    }

    function heart(b) {
        var id = b.getAttribute('data-heart');
        var s = savedList();
        var at = s.indexOf(id);
        var on = at < 0;
        if (on) {
            s.unshift(id);
        } else {
            s.splice(at, 1);
        }
        if (!savedStore(s)) {
            toast('Saving needs browser storage, which is turned off here.');
            return;
        }
        hearts();
        savedCount();
        toast(on ? 'Saved. Find your dogs under the heart at the top.' : 'Removed from saved dogs.');
        call('Heart', [{ key: 'hid', vlu: id }, { key: 'hon', vlu: on ? '1' : '0' }]);
        if (window.DogJssaved) {
            window.DogJssaved(id, on);
        }
    }

    function subscribe() {
        call('Subscribe', [{ key: 'nlmail', vlu: val('dd-nlmail') }]);
        return false;
    }

    function subscribed(msg) {
        el('dd-nlmail').value = '';
        toast(msg);
    }

    function fieldErr(id, msg) {
        var e = el('dd-e-' + id);
        if (e) {
            e.textContent = msg;
        }
        var f = el('dd-' + id);
        if (f && f.focus) {
            f.focus();
        }
    }

    var photos = [];
    var at = 0;
    var lbBack = null;
    var touchX = null;

    function paint() {
        var p = photos[at];
        if (!p) {
            return;
        }
        var img = el('dd-lbimg');
        var vid = el('dd-lbvid');
        vid.pause();
        if (p.v) {
            img.hidden = true;
            vid.hidden = false;
            vid.poster = p.t;
            vid.src = p.f;
            vid.play().catch(function () {
            });
        } else {
            vid.hidden = true;
            vid.removeAttribute('src');
            img.hidden = false;
            img.src = p.f;
            img.alt = p.c;
        }
        el('dd-lbn').textContent = (at + 1) + ' / ' + photos.length;
        el('dd-lbc').textContent = p.c;
        var list = el('dd-lbthumbs').children;
        for (var i = 0; i < list.length; i++) {
            list[i].classList.toggle('dd-act', i === at);
            list[i].setAttribute('aria-current', i === at ? 'true' : 'false');
        }
    }

    function show(i) {
        if (!photos.length) {
            return;
        }
        lbBack = document.activeElement;
        at = Math.max(0, Math.min(i || 0, photos.length - 1));
        el('dd-lb').hidden = false;
        document.documentElement.style.overflow = 'hidden';
        paint();
        el('dd-lb').querySelector('.dd-lbx').focus();
    }

    function hide() {
        el('dd-lbvid').pause();
        el('dd-lb').hidden = true;
        document.documentElement.style.overflow = '';
        if (lbBack && lbBack.focus) {
            lbBack.focus();
        }
    }

    function step(d) {
        at = (at + d + photos.length) % photos.length;
        paint();
    }

    function pick(i) {
        at = i;
        paint();
    }

    function share() {
        var url = location.href;
        var t = document.title;
        if (navigator.share) {
            navigator.share({ title: t, url: url }).catch(function () {
            });
            return;
        }
        try {
            navigator.clipboard.writeText(url).then(function () {
                toast('Link copied. Share it with a friend.');
            }, function () {
                toast(url);
            });
        } catch (e) {
            toast(url);
        }
    }

    function send() {
        el('dd-asend').disabled = true;
        var list = [];
        ['aname', 'amail', 'aphone', 'amsg', 'aweb', 'adog'].forEach(function (k) {
            list.push({ key: k, vlu: val('dd-' + k) });
        });
        call('Send', list);
        return false;
    }

    function sent(ok, text) {
        el('dd-asend').disabled = false;
        if (!ok) {
            return;
        }
        el('dd-af').hidden = true;
        el('dd-adone').innerHTML = '<div class="dd-ok-box" role="status"><b>Question sent</b><p></p></div>';
        el('dd-adone').querySelector('p').textContent = text;
    }

    function start() {
        var g = el('dd-gal');
        if (!g) {
            return;
        }
        try {
            photos = JSON.parse(g.getAttribute('data-photos') || '[]');
        } catch (e) {
            photos = [];
        }
        var th = '';
        photos.forEach(function (p, i) {
            th += '<button type="button" aria-label="' + (p.v ? 'Video' : 'Photo') + ' ' + (i + 1) + '" onclick="DogJs.pick(' + i + ')"><img src="' + esc(p.t) + '" alt="" loading="lazy">' + (p.v ? '<span class="dd-gplay"></span>' : '') + '</button>';
        });
        el('dd-lbthumbs').innerHTML = th;
        var lb = el('dd-lb');
        lb.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                e.stopPropagation();
                hide();
            } else if (e.key === 'ArrowLeft') {
                step(-1);
            } else if (e.key === 'ArrowRight') {
                step(1);
            }
        });
        var stage = lb.querySelector('.dd-lbstage');
        stage.addEventListener('touchstart', function (e) {
            touchX = e.touches[0].clientX;
        }, { passive: true });
        stage.addEventListener('touchend', function (e) {
            if (touchX == null) {
                return;
            }
            var dx = e.changedTouches[0].clientX - touchX;
            touchX = null;
            if (Math.abs(dx) > 40) {
                step(dx < 0 ? 1 : -1);
            }
        });
        stage.addEventListener('click', function (e) {
            if (e.target === stage) {
                hide();
            }
        });
    }

    function reveal() {
        var stored = '';
        try {
            stored = localStorage.getItem('wd-theme') || '';
        } catch (e) {
        }
        if (stored) {
            applyTheme(stored);
        }
        times();
        if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            [].forEach.call(document.querySelectorAll('video[data-bg]'), function (v) {
                v.removeAttribute('autoplay');
                v.pause();
            });
        }
        hearts();
        savedCount();
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
            }
        });
        document.addEventListener('input', function (e) {
            var id = e.target && e.target.id ? e.target.id.replace('dd-', 'dd-e-') : '';
            if (id && el(id)) {
                el(id).textContent = '';
            }
        });
        start();
    }

    return {
        reveal: reveal,
        toast: toast,
        openNav: openNav,
        closeNav: closeNav,
        toggleTheme: toggleTheme,
        heart: heart,
        subscribe: subscribe,
        subscribed: subscribed,
        fieldErr: fieldErr,
        show: show,
        hide: hide,
        step: step,
        pick: pick,
        share: share,
        send: send,
        sent: sent
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', DogJs.reveal);
} else {
    DogJs.reveal();
}
