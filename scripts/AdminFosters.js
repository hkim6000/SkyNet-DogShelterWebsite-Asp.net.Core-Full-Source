var AdminFostersJs = (function () {

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
        $ApiRequest('AdminFosters/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('fz-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('fz-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('fz-open');
        }, 3200);
    }

    function openNav() {
        el('fz-nav').classList.add('fz-open');
        el('fz-scrim').classList.add('fz-open');
    }

    function closeNav() {
        el('fz-nav').classList.remove('fz-open');
        el('fz-scrim').classList.remove('fz-open');
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
        el('fz-modal').classList.add('fz-open');
        times(el('fz-mbody'));
        var f = el('fz-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('fz-modal');
        if (m) {
            m.classList.remove('fz-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('fz-per')) {
            el('fz-per').value = k;
        }
        if (k !== 'range') {
            if (el('fz-from')) {
                el('fz-from').value = '';
            }
            if (el('fz-to')) {
                el('fz-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('fz-per') }, { key: 'pfrom', vlu: val('fz-from') }, { key: 'pto', vlu: val('fz-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('fz-main'));
        if (window.MutationObserver && el('fz-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('fz-main'));
                }, 30);
            }).observe(el('fz-main'), { childList: true, subtree: true });
        }
        if (window.AdminFostersJsloaded) {
            window.AdminFostersJsloaded();
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
        if (!askBox || !askBox.classList.contains('fz-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('fz-open');
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
        askBox.className = 'fz-ask';
        askBox.innerHTML = '<div class="fz-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="fz-err" role="alert"></em><div class="fz-askacts"><button type="button" class="fz-btn fz-btn-ghost fz-btn-sm"></button><button type="button" class="fz-btn fz-btn-sm"></button></div></div>';
        (el('fz-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('fz-open')) {
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
        x.b[1].className = 'fz-btn fz-btn-sm' + (o.danger ? ' fz-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('fz-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('fz-open');
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

    var current = '';
    var stay = '';
    var mode = 'homes';

    window.AdminFostersJsloaded = function () {
        if (qs('view') === 'stays') {
            mode = 'stays';
        }
        list(1);
        if (qs('id')) {
            open1(qs('id'));
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: (qs('view') === 'stays' ? 'stays' : 'homes') + '|' + qs('status') }];
    }

    function view(v) {
        mode = v;
        api('View', [{ key: 'fview', vlu: v }]);
    }

    function list(pg) {
        if (mode === 'stays') {
            api('Stays', [{ key: 'sq', vlu: val('fz-q') }, { key: 'sst', vlu: val('fz-sst') }, { key: 'pg', vlu: String(pg || 1) }]);
        } else {
            api('List', [{ key: 'fq', vlu: val('fz-q') }, { key: 'fst', vlu: val('fz-fst') }, { key: 'fsize', vlu: val('fz-fsize') }, { key: 'pg', vlu: String(pg || 1) }]);
        }
    }

    function open1(id) {
        current = id;
        api('Open', [{ key: 'fid', vlu: id }]);
    }

    function status(s) {
        api('SetStatus', [{ key: 'fid', vlu: current }, { key: 'fst2', vlu: s }]);
    }

    function place() {
        api('Place', [{ key: 'fid', vlu: current }, { key: 'pdog', vlu: val('fz-pdog') }, { key: 'pstart', vlu: val('fz-pstart') }, { key: 'puntil', vlu: val('fz-puntil') }, { key: 'pnote', vlu: val('fz-pnote') }]);
    }

    function note() {
        api('AddNote', [{ key: 'fid', vlu: current }, { key: 'enote', vlu: val('fz-enote') }]);
    }

    function changed(msg) {
        if (current) {
            open1(current);
        }
        list(1);
        if (msg) {
            toast(msg);
        }
    }

    function stay1(id) {
        stay = id;
        current = '';
        api('Stay', [{ key: 'sid', vlu: id }]);
    }

    function saveStay() {
        api('SaveStay', [{ key: 'sid', vlu: stay }, { key: 'suntil', vlu: val('fz-suntil') }, { key: 'snote', vlu: val('fz-snote') }]);
    }

    function endStay(dog, home) {
        ask({ title: 'End ' + dog + "'s stay with " + home + '?', text: 'Use this when the dog comes back to the shelter or goes home with an adopter. A dog shown as In foster goes back to Available.', ok: 'End stay', no: 'Back' }, function () {
            api('EndStay', [{ key: 'sid', vlu: stay }, { key: 'send', vlu: val('fz-send') }]);
        });
    }

    function stayDone(msg) {
        closeModal();
        list(1);
        toast(msg);
    }

    function remove(name) {
        ask({ title: 'Delete ' + name + '?', text: 'The foster home and its notes are removed. Past stays stay in each dog record.', ok: 'Delete', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'fid', vlu: current }]);
        });
    }

    function deleted() {
        current = '';
        closeModal();
        list(1);
        toast('Foster home deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('fz-', 'fz-e-') : '';
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
        view: view,
        list: list,
        open: open1,
        status: status,
        place: place,
        note: note,
        changed: changed,
        stay: stay1,
        saveStay: saveStay,
        endStay: endStay,
        stayDone: stayDone,
        remove: remove,
        deleted: deleted
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminFostersJs.reveal);
} else {
    AdminFostersJs.reveal();
}
