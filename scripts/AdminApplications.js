var AdminApplicationsJs = (function () {

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
        $ApiRequest('AdminApplications/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('ap-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('ap-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ap-open');
        }, 3200);
    }

    function openNav() {
        el('ap-nav').classList.add('ap-open');
        el('ap-scrim').classList.add('ap-open');
    }

    function closeNav() {
        el('ap-nav').classList.remove('ap-open');
        el('ap-scrim').classList.remove('ap-open');
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
        el('ap-modal').classList.add('ap-open');
        times(el('ap-mbody'));
        var f = el('ap-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('ap-modal');
        if (m) {
            m.classList.remove('ap-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('ap-per')) {
            el('ap-per').value = k;
        }
        if (k !== 'range') {
            if (el('ap-from')) {
                el('ap-from').value = '';
            }
            if (el('ap-to')) {
                el('ap-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('ap-per') }, { key: 'pfrom', vlu: val('ap-from') }, { key: 'pto', vlu: val('ap-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('ap-main'));
        if (window.MutationObserver && el('ap-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('ap-main'));
                }, 30);
            }).observe(el('ap-main'), { childList: true, subtree: true });
        }
        if (window.AdminApplicationsJsloaded) {
            window.AdminApplicationsJsloaded();
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
        if (!askBox || !askBox.classList.contains('ap-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('ap-open');
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
        askBox.className = 'ap-ask';
        askBox.innerHTML = '<div class="ap-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="ap-err" role="alert"></em><div class="ap-askacts"><button type="button" class="ap-btn ap-btn-ghost ap-btn-sm"></button><button type="button" class="ap-btn ap-btn-sm"></button></div></div>';
        (el('ap-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('ap-open')) {
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
        x.b[1].className = 'ap-btn ap-btn-sm' + (o.danger ? ' ap-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('ap-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('ap-open');
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

    window.AdminApplicationsJsloaded = function () {
        list(1);
        if (qs('id')) {
            open1(qs('id'));
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: qs('status') }];
    }

    function list(pg) {
        api('List', [{ key: 'aq', vlu: val('ap-q') }, { key: 'ast', vlu: val('ap-ast') }, { key: 'adog', vlu: val('ap-adog') }, { key: 'arev', vlu: val('ap-arev') }, { key: 'pg', vlu: String(pg || 1) }].concat(period()));
    }

    function open1(id) {
        current = id;
        api('Open', [{ key: 'aid', vlu: id }]);
    }

    function status(s) {
        api('SetStatus', [{ key: 'aid', vlu: current }, { key: 'ast2', vlu: s }]);
    }

    function decide(s, who, dog) {
        var o = {
            approved: { title: 'Approve ' + who + '?', text: dog ? dog + ' is marked Adoption pending on the website.' : 'You can choose a dog with them later.', ok: 'Approve' },
            declined: { title: 'Decline ' + who + '?', text: 'Let them know by phone or email. The note is kept with the application.', ok: 'Decline', danger: true },
            adopted: { title: 'Mark ' + (dog || 'the dog') + ' as adopted by ' + who + '?', text: 'The dog leaves the website and moves to Happy tails. Other open applications for this dog get a note.', ok: 'Mark adopted' }
        }[s];
        ask({ title: o.title, text: o.text, ok: o.ok, no: 'Back', danger: !!o.danger }, function () {
            api('Decide', [{ key: 'aid', vlu: current }, { key: 'ast2', vlu: s }]);
        });
    }

    function reviewer() {
        api('Assign', [{ key: 'aid', vlu: current }, { key: 'arto', vlu: val('ap-arto') }]);
    }

    function visit() {
        api('Visit', [{ key: 'aid', vlu: current }, { key: 'avisit', vlu: val('ap-avisit') }]);
    }

    function note() {
        api('AddNote', [{ key: 'aid', vlu: current }, { key: 'enote', vlu: val('ap-enote') }]);
    }

    function changed(msg) {
        open1(current);
        list(1);
        if (msg) {
            toast(msg);
        }
    }

    function remove(name) {
        ask({ title: 'Delete the application from ' + name + '?', text: 'Their details, answers and notes are removed for good.', ok: 'Delete application', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'aid', vlu: current }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Application deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('ap-', 'ap-e-') : '';
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
        open: open1,
        status: status,
        decide: decide,
        reviewer: reviewer,
        visit: visit,
        note: note,
        changed: changed,
        remove: remove,
        deleted: deleted
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminApplicationsJs.reveal);
} else {
    AdminApplicationsJs.reveal();
}
