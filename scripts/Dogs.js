var DogsJs = (function () {

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
        $ApiRequest('Dogs/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('dg-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('dg-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('dg-open');
        }, 3200);
    }

    function openNav() {
        el('dg-nav').classList.add('dg-open');
        el('dg-scrim').classList.add('dg-open');
    }

    function closeNav() {
        el('dg-nav').classList.remove('dg-open');
        el('dg-scrim').classList.remove('dg-open');
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
        if (!askBox || !askBox.classList.contains('dg-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('dg-open');
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
        askBox.className = 'dg-ask';
        askBox.innerHTML = '<div class="dg-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="dg-err" role="alert"></em><div class="dg-askacts"><button type="button" class="dg-btn dg-btn-ghost dg-btn-sm"></button><button type="button" class="dg-btn dg-btn-sm"></button></div></div>';
        (el('dg-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('dg-open')) {
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
        x.b[1].className = 'dg-btn dg-btn-sm' + (o.danger ? ' dg-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('dg-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('dg-open');
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
        el('dg-site').setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
    }

    function toggleTheme() {
        var next = el('dg-site').getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
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
        var c = el('dg-scount');
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
        if (window.DogsJssaved) {
            window.DogsJssaved(id, on);
        }
    }

    function subscribe() {
        call('Subscribe', [{ key: 'nlmail', vlu: val('dg-nlmail') }]);
        return false;
    }

    function subscribed(msg) {
        el('dg-nlmail').value = '';
        toast(msg);
    }

    function fieldErr(id, msg) {
        var e = el('dg-e-' + id);
        if (e) {
            e.textContent = msg;
        }
        var f = el('dg-' + id);
        if (f && f.focus) {
            f.focus();
        }
    }

    var qtimer = null;
    var curPage = 1;

    function args() {
        var a = [];
        ['q', 'size', 'age', 'sex', 'good', 'energy', 'sort'].forEach(function (k) {
            a.push({ key: k, vlu: val('dg-' + k) });
        });
        a.push({ key: 'pending', vlu: el('dg-pending').checked ? '1' : '0' });
        a.push({ key: 'saved', vlu: val('dg-saved') });
        a.push({ key: 'ids', vlu: savedList().join(',') });
        a.push({ key: 'pg', vlu: String(curPage) });
        return a;
    }

    function url() {
        var u = new URLSearchParams();
        args().forEach(function (x) {
            if (x.key === 'ids' || !x.vlu || (x.key === 'sort' && x.vlu === 'new') || (x.key === 'pending' && x.vlu === '1') || (x.key === 'pg' && x.vlu === '1')) {
                return;
            }
            u.set(x.key, x.vlu);
        });
        var s = u.toString();
        try {
            history.replaceState(null, '', 'Dogs' + (s ? '?' + s : ''));
        } catch (e) {
        }
    }

    function go() {
        curPage = 1;
        url();
        call('Search', args());
    }

    function typed() {
        clearTimeout(qtimer);
        qtimer = setTimeout(go, 300);
    }

    function page(n) {
        curPage = n;
        url();
        call('Search', args());
        var r = el('dg-results');
        window.scrollTo({ top: r.getBoundingClientRect().top + window.scrollY - 140, behavior: 'smooth' });
    }

    function shown() {
        hearts(el('dg-results'));
    }

    function clearAll() {
        ['q', 'size', 'age', 'sex', 'good', 'energy'].forEach(function (k) {
            el('dg-' + k).value = '';
        });
        el('dg-sort').value = 'new';
        el('dg-pending').checked = true;
        el('dg-saved').value = '';
        go();
    }

    window.DogsJssaved = function () {
        if (val('dg-saved') === '1') {
            call('Search', args());
        }
    };

    function start() {
        curPage = +(qs('pg') || 1) || 1;
        if (val('dg-saved') === '1') {
            call('Search', args());
        }
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
            var id = e.target && e.target.id ? e.target.id.replace('dg-', 'dg-e-') : '';
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
        go: go,
        typed: typed,
        page: page,
        shown: shown,
        clearAll: clearAll
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', DogsJs.reveal);
} else {
    DogsJs.reveal();
}
