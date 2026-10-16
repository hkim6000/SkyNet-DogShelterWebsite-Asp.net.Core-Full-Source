var AdminVolunteersJs = (function () {

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
        $ApiRequest('AdminVolunteers/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('vz-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('vz-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('vz-open');
        }, 3200);
    }

    function openNav() {
        el('vz-nav').classList.add('vz-open');
        el('vz-scrim').classList.add('vz-open');
    }

    function closeNav() {
        el('vz-nav').classList.remove('vz-open');
        el('vz-scrim').classList.remove('vz-open');
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
        el('vz-modal').classList.add('vz-open');
        times(el('vz-mbody'));
        var f = el('vz-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('vz-modal');
        if (m) {
            m.classList.remove('vz-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('vz-per')) {
            el('vz-per').value = k;
        }
        if (k !== 'range') {
            if (el('vz-from')) {
                el('vz-from').value = '';
            }
            if (el('vz-to')) {
                el('vz-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('vz-per') }, { key: 'pfrom', vlu: val('vz-from') }, { key: 'pto', vlu: val('vz-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('vz-main'));
        if (window.MutationObserver && el('vz-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('vz-main'));
                }, 30);
            }).observe(el('vz-main'), { childList: true, subtree: true });
        }
        if (window.AdminVolunteersJsloaded) {
            window.AdminVolunteersJsloaded();
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
        if (!askBox || !askBox.classList.contains('vz-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('vz-open');
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
        askBox.className = 'vz-ask';
        askBox.innerHTML = '<div class="vz-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="vz-err" role="alert"></em><div class="vz-askacts"><button type="button" class="vz-btn vz-btn-ghost vz-btn-sm"></button><button type="button" class="vz-btn vz-btn-sm"></button></div></div>';
        (el('vz-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('vz-open')) {
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
        x.b[1].className = 'vz-btn vz-btn-sm' + (o.danger ? ' vz-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('vz-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('vz-open');
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

    window.AdminVolunteersJsloaded = function () {
        list(1);
        if (qs('id')) {
            open1(qs('id'));
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: qs('status') }];
    }

    function list(pg) {
        api('List', [{ key: 'vq', vlu: val('vz-q') }, { key: 'vst', vlu: val('vz-vst') }, { key: 'vrole', vlu: val('vz-vrole') }, { key: 'vtime', vlu: val('vz-vtime') }, { key: 'pg', vlu: String(pg || 1) }]);
    }

    function open1(id) {
        current = id;
        api('Open', [{ key: 'vid', vlu: id }]);
    }

    function status(s) {
        api('SetStatus', [{ key: 'vid', vlu: current }, { key: 'vst2', vlu: s }]);
    }

    function logHours() {
        api('LogHours', [{ key: 'vid', vlu: current }, { key: 'hday', vlu: val('vz-hday') }, { key: 'hnum', vlu: val('vz-hnum') }, { key: 'hnote', vlu: val('vz-hnote') }]);
    }

    function dropHours(i) {
        ask({ title: 'Remove this entry?', text: 'The hours come off the total.', ok: 'Remove', no: 'Keep it', danger: true }, function () {
            api('DropHours', [{ key: 'vid', vlu: current }, { key: 'hi', vlu: String(i) }]);
        });
    }

    function note() {
        api('AddNote', [{ key: 'vid', vlu: current }, { key: 'enote', vlu: val('vz-enote') }]);
    }

    function changed(msg) {
        open1(current);
        list(1);
        if (msg) {
            toast(msg);
        }
    }

    function form(id) {
        api('Form', [{ key: 'vid', vlu: id || '' }]);
    }

    function picked(n) {
        return [].map.call(document.querySelectorAll('#vz-mbody input[name="vz-' + n + '"]:checked'), function (x) {
            return x.value;
        }).join(',');
    }

    function saveVol(id) {
        var list2 = ['fname', 'fmail', 'fphone', 'fstat', 'fmsg'].map(function (k) {
            return { key: k, vlu: el('vz-' + k) ? el('vz-' + k).value : '' };
        });
        list2.push({ key: 'vid', vlu: id || '' });
        list2.push({ key: 'froles', vlu: picked('frole') });
        list2.push({ key: 'ftimes', vlu: picked('ftime') });
        list2.push({ key: 'fadult', vlu: el('vz-fadult').checked ? '1' : '0' });
        api('SaveVol', list2);
    }

    function volFail(msg) {
        el('vz-e-vol').textContent = msg;
    }

    function volSaved(id, isNew) {
        current = id;
        list(1);
        open1(id);
        toast(isNew ? 'Volunteer added.' : 'Saved.');
    }

    function remove(name) {
        ask({ title: 'Delete ' + name + '?', text: 'Their sign-up, hours and notes are removed for good.', ok: 'Delete', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'vid', vlu: current }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Volunteer deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('vz-', 'vz-e-') : '';
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
        logHours: logHours,
        dropHours: dropHours,
        note: note,
        changed: changed,
        form: form,
        saveVol: saveVol,
        volFail: volFail,
        volSaved: volSaved,
        remove: remove,
        deleted: deleted
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminVolunteersJs.reveal);
} else {
    AdminVolunteersJs.reveal();
}
