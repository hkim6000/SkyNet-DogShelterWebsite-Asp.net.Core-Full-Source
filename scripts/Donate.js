var DonateJs = (function () {

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
        $ApiRequest('Donate/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('dn-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('dn-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('dn-open');
        }, 3200);
    }

    function openNav() {
        el('dn-nav').classList.add('dn-open');
        el('dn-scrim').classList.add('dn-open');
    }

    function closeNav() {
        el('dn-nav').classList.remove('dn-open');
        el('dn-scrim').classList.remove('dn-open');
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
        if (!askBox || !askBox.classList.contains('dn-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('dn-open');
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
        askBox.className = 'dn-ask';
        askBox.innerHTML = '<div class="dn-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="dn-err" role="alert"></em><div class="dn-askacts"><button type="button" class="dn-btn dn-btn-ghost dn-btn-sm"></button><button type="button" class="dn-btn dn-btn-sm"></button></div></div>';
        (el('dn-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('dn-open')) {
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
        x.b[1].className = 'dn-btn dn-btn-sm' + (o.danger ? ' dn-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('dn-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('dn-open');
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
        el('dn-site').setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
    }

    function toggleTheme() {
        var next = el('dn-site').getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
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
        var c = el('dn-scount');
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
        if (window.DonateJssaved) {
            window.DonateJssaved(id, on);
        }
    }

    function subscribe() {
        call('Subscribe', [{ key: 'nlmail', vlu: val('dn-nlmail') }]);
        return false;
    }

    function subscribed(msg) {
        el('dn-nlmail').value = '';
        toast(msg);
    }

    function fieldErr(id, msg) {
        var e = el('dn-e-' + id);
        if (e) {
            e.textContent = msg;
        }
        var f = el('dn-' + id);
        if (f && f.focus) {
            f.focus();
        }
    }

    function amount() {
        var o = val('dn-oamt').replace(/[^0-9.]/g, '');
        if (o) {
            return o;
        }
        var r = document.querySelector('input[name="dn-amt"]:checked');
        return r ? r.value : '';
    }

    function sum() {
        var a = parseFloat(amount());
        var every = document.querySelector('input[name="dn-every"]:checked');
        el('dn-sumtxt').textContent = a > 0 ? 'Pledge $' + a.toLocaleString('en-US', { maximumFractionDigits: 2 }) + (every && every.value === 'monthly' ? ' a month' : '') : 'Pledge a gift';
    }

    function amt() {
        el('dn-oamt').value = '';
        el('dn-e-oamt').textContent = '';
        sum();
    }

    function other() {
        if (val('dn-oamt')) {
            var r = document.querySelectorAll('input[name="dn-amt"]');
            for (var i = 0; i < r.length; i++) {
                r[i].checked = false;
            }
        }
        sum();
    }

    function fund() {
        el('dn-dogw').hidden = val('dn-fund') !== 'sponsor';
        el('dn-e-dog').textContent = '';
    }

    function send() {
        el('dn-send').disabled = true;
        var list = [{ key: 'amount', vlu: amount() }];
        ['fund', 'dog', 'honor', 'name', 'mail', 'phone', 'msg', 'web'].forEach(function (k) {
            list.push({ key: k, vlu: val('dn-' + k) });
        });
        var e = document.querySelector('input[name="dn-every"]:checked');
        var m = document.querySelector('input[name="dn-method"]:checked');
        list.push({ key: 'every', vlu: e ? e.value : 'once' });
        list.push({ key: 'method', vlu: m ? m.value : 'check' });
        list.push({ key: 'anon', vlu: el('dn-anon').checked ? '1' : '0' });
        call('Send', list);
        return false;
    }

    function sent(ok, first, id, what, how) {
        el('dn-send').disabled = false;
        if (!ok) {
            return;
        }
        el('dn-form').hidden = true;
        var d = el('dn-done');
        d.innerHTML = '<div class="dn-ok-box" role="status"><b></b><p></p><p></p><p></p></div>';
        var p = d.querySelectorAll('p');
        d.querySelector('b').textContent = 'Thank you, ' + first + '!';
        p[0].textContent = 'You pledged ' + what + '.';
        p[1].innerHTML = 'Your pledge number is <span class="dn-code"></span>.';
        p[1].querySelector('span').textContent = id || 'on its way';
        p[2].textContent = how;
        window.scrollTo({ top: d.getBoundingClientRect().top + window.scrollY - 120, behavior: 'smooth' });
    }

    function start() {
        document.addEventListener('change', function (e) {
            if (e.target && e.target.name === 'dn-every') {
                sum();
            }
        });
        sum();
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
            var id = e.target && e.target.id ? e.target.id.replace('dn-', 'dn-e-') : '';
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
        amt: amt,
        other: other,
        fund: fund,
        send: send,
        sent: sent
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', DonateJs.reveal);
} else {
    DonateJs.reveal();
}
