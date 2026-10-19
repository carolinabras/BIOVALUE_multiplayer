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
  }
});
