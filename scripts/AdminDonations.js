var AdminDonationsJs = (function () {

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
        $ApiRequest('AdminDonations/' + method, JSON.stringify(list || []));
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, secs) {
        document.cookie = k + '=' + encodeURIComponent(v) + '; path=' + BASE + '; max-age=' + secs + '; samesite=lax';
    }

    function toast(msg) {
        var t = el('dm-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('dm-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('dm-open');
        }, 3200);
    }

    function openNav() {
        el('dm-nav').classList.add('dm-open');
        el('dm-scrim').classList.add('dm-open');
    }

    function closeNav() {
        el('dm-nav').classList.remove('dm-open');
        el('dm-scrim').classList.remove('dm-open');
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
        el('dm-modal').classList.add('dm-open');
        times(el('dm-mbody'));
        var f = el('dm-mbody').querySelector('input:not([type="hidden"]):not([readonly]), select, textarea');
        if (f && window.innerWidth > 640) {
            f.focus();
        }
    }

    function closeModal() {
        var m = el('dm-modal');
        if (m) {
            m.classList.remove('dm-open');
        }
    }

    function qs(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function per(k) {
        if (el('dm-per')) {
            el('dm-per').value = k;
        }
        if (k !== 'range') {
            if (el('dm-from')) {
                el('dm-from').value = '';
            }
            if (el('dm-to')) {
                el('dm-to').value = '';
            }
        }
        list(1);
    }

    function period() {
        return [{ key: 'per', vlu: val('dm-per') }, { key: 'pfrom', vlu: val('dm-from') }, { key: 'pto', vlu: val('dm-to') }];
    }

    function search() {
        clearTimeout(stimer);
        stimer = setTimeout(function () {
            list(1);
        }, 320);
    }

    function loaded() {
        times(el('dm-main'));
        if (window.MutationObserver && el('dm-main')) {
            new MutationObserver(function () {
                clearTimeout(otimer);
                otimer = setTimeout(function () {
                    times(el('dm-main'));
                }, 30);
            }).observe(el('dm-main'), { childList: true, subtree: true });
        }
        if (window.AdminDonationsJsloaded) {
            window.AdminDonationsJsloaded();
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
        if (!askBox || !askBox.classList.contains('dm-open')) {
            return;
        }
        var x = askParts();
        var f = askDone;
        var v = x.i.value.trim();
        askBox.classList.remove('dm-open');
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
        askBox.className = 'dm-ask';
        askBox.innerHTML = '<div class="dm-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" spellcheck="false"><em class="dm-err" role="alert"></em><div class="dm-askacts"><button type="button" class="dm-btn dm-btn-ghost dm-btn-sm"></button><button type="button" class="dm-btn dm-btn-sm"></button></div></div>';
        (el('dm-site') || document.body).appendChild(askBox);
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
            if (!askBox.classList.contains('dm-open')) {
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
        x.b[1].className = 'dm-btn dm-btn-sm' + (o.danger ? ' dm-askdel' : '');
        var input = typeof o.value === 'string';
        askBox.firstChild.classList.toggle('dm-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        x.i.setAttribute('aria-label', o.title || '');
        askBox.classList.add('dm-open');
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

    window.AdminDonationsJsloaded = function () {
        list(1);
        var id = qs('id');
        if (id === 'new') {
            add();
        } else if (id) {
            open1(id);
        }
    };

    function loadArgs() {
        return [{ key: 'arg', vlu: qs('status') }];
    }

    function filters() {
        return [{ key: 'dq', vlu: val('dm-q') }, { key: 'dst', vlu: val('dm-dst') }, { key: 'dfund', vlu: val('dm-dfund') }, { key: 'dmeth', vlu: val('dm-dmeth') }, { key: 'devery', vlu: val('dm-devery') }].concat(period());
    }

    function list(pg) {
        api('List', filters().concat([{ key: 'pg', vlu: String(pg || 1) }]));
    }

    function exportCsv() {
        api('Export', filters());
    }

    function download(name, text) {
        var blob = new Blob(['﻿' + text], { type: 'text/csv;charset=utf-8' });
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = name;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () {
            URL.revokeObjectURL(a.href);
            a.remove();
        }, 500);
        toast('Download started.');
    }

    function open1(id) {
        current = id;
        api('Open', [{ key: 'did', vlu: id }]);
    }

    function receive() {
        api('SetStatus', [{ key: 'did', vlu: current }, { key: 'dst2', vlu: 'received' }, { key: 'dday', vlu: val('dm-dday') }]);
    }

    function status(s, who) {
        if (s === 'cancelled') {
            ask({ title: 'Cancel the pledge from ' + who + '?', text: 'It stays on record as Cancelled and no longer counts toward open pledges.', ok: 'Cancel pledge', no: 'Back', danger: true }, function () {
                api('SetStatus', [{ key: 'did', vlu: current }, { key: 'dst2', vlu: s }]);
            });
            return;
        }
        api('SetStatus', [{ key: 'did', vlu: current }, { key: 'dst2', vlu: s }]);
    }

    function note() {
        api('AddNote', [{ key: 'did', vlu: current }, { key: 'enote', vlu: val('dm-enote') }]);
    }

    function changed(msg) {
        open1(current);
        list(1);
        if (msg) {
            toast(msg);
        }
    }

    function add() {
        api('AddForm', []);
    }

    function fundView() {
        var f = el('dm-nfund');
        var d = el('dm-ndogw');
        if (f && d) {
            d.hidden = f.value !== 'sponsor';
        }
    }

    function saveNew() {
        var keys = ['nname', 'nmail', 'nphone', 'namt', 'nevery', 'nfund', 'ndog', 'nmeth', 'nday', 'nhonor', 'nmsg'];
        var list2 = keys.map(function (k) {
            return { key: k, vlu: el('dm-' + k) ? el('dm-' + k).value : '' };
        });
        list2.push({ key: 'nanon', vlu: el('dm-nanon') && el('dm-nanon').checked ? '1' : '0' });
        api('AddGift', list2);
    }

    function failNew(msg) {
        el('dm-e-new').textContent = msg;
    }

    function added(id) {
        current = id;
        list(1);
        open1(id);
        toast('Gift recorded.');
        try {
            history.replaceState(null, '', 'AdminDonations');
        } catch (e) {
        }
    }

    function remove(name) {
        ask({ title: 'Delete the gift from ' + name + '?', text: 'Use this only for mistakes or duplicates. To keep the record, cancel the pledge instead.', ok: 'Delete', no: 'Keep it', danger: true }, function () {
            api('Delete', [{ key: 'did', vlu: current }]);
        });
    }

    function deleted() {
        closeModal();
        list(1);
        toast('Gift deleted.');
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
            var id = e.target && e.target.id ? e.target.id.replace('dm-', 'dm-e-') : '';
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
        exportCsv: exportCsv,
        download: download,
        open: open1,
        receive: receive,
        status: status,
        note: note,
        changed: changed,
        add: add,
        fundView: fundView,
        saveNew: saveNew,
        failNew: failNew,
        added: added,
        remove: remove,
        deleted: deleted
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminDonationsJs.reveal);
} else {
    AdminDonationsJs.reveal();
}
