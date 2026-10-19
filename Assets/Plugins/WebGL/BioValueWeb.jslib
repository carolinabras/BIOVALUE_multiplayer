// Browser helpers for the web build (WebBridge.cs): copy text, download a text file.
mergeInto(LibraryManager.library, {
  BioValue_CopyText: function (textPtr) {
    var text = UTF8ToString(textPtr);
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).catch(function () { BioValue_FallbackCopy(text); });
    } else {
      BioValue_FallbackCopy(text);
    }
    function BioValue_FallbackCopy(t) {
      var area = document.createElement('textarea');
      area.value = t;
      area.style.position = 'fixed';
      area.style.opacity = '0';
      document.body.appendChild(area);
      area.select();
      try { document.execCommand('copy'); } catch (e) {}
      document.body.removeChild(area);
    }
  },

  BioValue_DownloadText: function (namePtr, textPtr) {
    var name = UTF8ToString(namePtr);
    var text = UTF8ToString(textPtr);
    var blob = new Blob([text], { type: 'text/plain;charset=utf-8' });
    var url = URL.createObjectURL(blob);
    var link = document.createElement('a');
    link.href = url;
    link.download = name;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
  },

  // "Quit" in the browser: leave full screen, stop the game and say goodbye over it.
  BioValue_Quit: function () {
    try { if (document.fullscreenElement) document.exitFullscreen(); } catch (e) {}
    var canvas = Module.canvas;
    var host = (canvas && canvas.parentElement) || document.body;
    var page = document.createElement('div');
    page.style.cssText = 'position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;' +
      'justify-content:center;gap:12px;background:#231942;color:#fff;font-family:sans-serif;text-align:center;z-index:10';
    page.innerHTML = '<div style="font-size:32px;font-weight:700">Thanks for playing BioValue</div>' +
      '<div style="font-size:18px;opacity:.7">You can close this tab now.</div>';
    host.appendChild(page);
    if (canvas) canvas.style.visibility = 'hidden';
    try { if (typeof Module.pauseMainLoop === 'function') Module.pauseMainLoop(); } catch (e) {}
  }
});
