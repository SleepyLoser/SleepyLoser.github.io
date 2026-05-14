/**
 * adaptive-cover.js
 * 作用：让 Butterfly 主题的 #page-header（top_img 顶图）高度根据真实图片宽高比自适应
 * 原理：
 *   1. 读取 #page-header 的 background-image URL；
 *   2. 用 Image 对象加载该图片，拿到 naturalWidth/naturalHeight；
 *   3. 把 (高/宽) 写到 CSS 变量 --top-img-ratio，CSS 用 calc(100vw * var(--top-img-ratio)) 动态算高度。
 * 兼容：支持 pjax 切页（监听 pjax:complete）
 */
(function () {
  'use strict';

  // 从 background-image 字符串里提取 url
  function extractUrl(bgImage) {
    if (!bgImage || bgImage === 'none') return null;
    var match = bgImage.match(/url\((['"]?)(.*?)\1\)/);
    return match ? match[2] : null;
  }

  function applyAdaptiveHeight() {
    var header = document.getElementById('page-header');
    if (!header) return;

    // 仅对带有 top_img 的页面生效（post-bg 或 full_page）
    if (!header.classList.contains('post-bg') && !header.classList.contains('full_page')) {
      return;
    }

    var bg = window.getComputedStyle(header).backgroundImage;
    var url = extractUrl(bg);
    if (!url) return;

    var img = new Image();
    img.onload = function () {
      if (!img.naturalWidth) return;
      var ratio = img.naturalHeight / img.naturalWidth;
      // 写入 CSS 变量，CSS 中 height = 100vw * ratio
      header.style.setProperty('--top-img-ratio', ratio.toFixed(6));
    };
    img.onerror = function () {
      // 加载失败则不处理，保留主题原始样式
    };
    img.src = url;
  }

  // 初次加载
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', applyAdaptiveHeight);
  } else {
    applyAdaptiveHeight();
  }

  // 兼容 pjax 切页
  document.addEventListener('pjax:complete', applyAdaptiveHeight);
  // 兼容 Butterfly 自带的 pjax 事件（部分版本叫这个）
  window.addEventListener('pjax:success', applyAdaptiveHeight);
})();
