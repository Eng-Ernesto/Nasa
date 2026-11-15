// Janela do terminal (build WebGL): abre a aplicação web Lunar Eyes numa janela
// por cima do canvas do jogo, dentro da própria página. Nunca abre outro separador.
// Fechar (botão ou Esc) avisa o Unity via SendMessage(go, 'OnFrameClosed').
mergeInto(LibraryManager.library, {
  LunarEyes_OpenFrame: function (urlPtr, goPtr) {
    var url = UTF8ToString(urlPtr);
    var go = UTF8ToString(goPtr);
    var send = function (method) {
      try {
        if (typeof SendMessage === 'function') SendMessage(go, method);
        else if (Module.SendMessage) Module.SendMessage(go, method);
      } catch (e) { console.warn('[LunarEyes] SendMessage falhou', e); }
    };

    if (window.__lunarEyesTerminal) window.__lunarEyesTerminal.destroy(false);

    var css = document.getElementById('lunareyes-terminal-style');
    if (!css) {
      css = document.createElement('style');
      css.id = 'lunareyes-terminal-style';
      css.textContent =
        '#lunareyes-terminal{position:fixed;z-index:1000;display:flex;align-items:center;justify-content:center;' +
        'background:rgba(8,17,27,.62);opacity:0;transition:opacity .25s ease;font-family:Inter,system-ui,sans-serif}' +
        '#lunareyes-terminal.le-open{opacity:1}' +
        '#lunareyes-terminal .le-win{position:relative;display:flex;flex-direction:column;width:min(1320px,94%);height:90%;' +
        'background:#08111B;border:1px solid #2B4054;border-radius:12px;overflow:hidden;' +
        'box-shadow:0 24px 80px rgba(0,0,0,.55),0 0 0 1px rgba(54,197,216,.08);transform:translateY(12px) scale(.985);transition:transform .3s ease}' +
        '#lunareyes-terminal.le-open .le-win{transform:none}' +
        '#lunareyes-terminal .le-bar{flex:0 0 52px;display:flex;align-items:center;gap:12px;padding:0 12px 0 18px;background:#101D2C;border-bottom:1px solid #2B4054}' +
        '#lunareyes-terminal .le-dot{width:8px;height:8px;border-radius:50%;background:#36C5D8;box-shadow:0 0 10px #36C5D8}' +
        '#lunareyes-terminal .le-title{flex:1;color:#AAB8C5;font:500 12px "JetBrains Mono",ui-monospace,monospace;letter-spacing:.12em;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}' +
        '#lunareyes-terminal .le-close{display:flex;align-items:center;gap:10px;height:36px;padding:0 14px;border-radius:8px;border:1px solid #2B4054;' +
        'background:#162638;color:#F4F6F3;font:500 13px Inter,system-ui,sans-serif;cursor:pointer}' +
        '#lunareyes-terminal .le-close:hover{border-color:#36C5D8}' +
        '#lunareyes-terminal .le-key{padding:2px 6px;border-radius:4px;border:1px solid #2B4054;color:#AAB8C5;font:500 11px "JetBrains Mono",ui-monospace,monospace}' +
        '#lunareyes-terminal iframe{flex:1;width:100%;border:0;background:#08111B}' +
        '#lunareyes-terminal .le-loading{position:absolute;inset:52px 0 0 0;display:flex;align-items:center;justify-content:center;' +
        'color:#AAB8C5;font:500 12px "JetBrains Mono",ui-monospace,monospace;letter-spacing:.12em;background:#08111B;pointer-events:none;transition:opacity .3s}';
      document.head.appendChild(css);
    }

    var wrap = document.createElement('div');
    wrap.id = 'lunareyes-terminal';
    wrap.innerHTML =
      '<div class="le-win" role="dialog" aria-label="Terminal Lunar Eyes">' +
      '<div class="le-bar"><span class="le-dot"></span><span class="le-title">TERMINAL DE OBSERVAÇÃO · LUNAR EYES</span>' +
      '<button type="button" class="le-close"><span class="le-key">Esc</span>Sair do terminal</button></div>' +
      '<iframe title="Lunar Eyes" allow="fullscreen; clipboard-write"></iframe>' +
      '<div class="le-loading">A LIGAR À APLICAÇÃO…</div></div>';

    // Cobre apenas a área do jogo (canvas), não a página inteira.
    var canvas = Module.canvas || document.querySelector('canvas');
    var fit = function () {
      var r = canvas ? canvas.getBoundingClientRect() : { left: 0, top: 0, width: innerWidth, height: innerHeight };
      wrap.style.left = r.left + 'px';
      wrap.style.top = r.top + 'px';
      wrap.style.width = r.width + 'px';
      wrap.style.height = r.height + 'px';
    };

    var frame = wrap.querySelector('iframe');
    var loading = wrap.querySelector('.le-loading');
    frame.addEventListener('load', function () { loading.style.opacity = '0'; });
    frame.src = url;

    var onKey = function (e) {
      if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); api.destroy(true); }
    };
    var api = {
      destroy: function (notify) {
        window.removeEventListener('resize', fit);
        window.removeEventListener('keydown', onKey, true);
        wrap.remove();
        if (window.__lunarEyesTerminal === api) window.__lunarEyesTerminal = null;
        if (canvas && canvas.focus) canvas.focus();
        if (notify) send('OnFrameClosed');
      }
    };
    wrap.querySelector('.le-close').onclick = function () { api.destroy(true); };
    window.addEventListener('resize', fit);
    window.addEventListener('keydown', onKey, true);
    window.__lunarEyesTerminal = api;

    if (document.pointerLockElement) document.exitPointerLock();
    fit();
    document.body.appendChild(wrap);
    requestAnimationFrame(function () { wrap.classList.add('le-open'); frame.focus(); });
  },

  LunarEyes_CloseFrame: function () {
    if (window.__lunarEyesTerminal) window.__lunarEyesTerminal.destroy(false);
  }
});
