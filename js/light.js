// 霓虹灯效果 - 修复版
// 颜色数组
var arr = ["#39c5bb", "#f14747", "#f1a247", "#f1ee47", "#b347f1", "#1edbff", "#ed709b", "#5636ed"];
// 颜色索引
var idx = 0;

// 切换颜色
function changeColor() {
  // 仅夜间模式才启用
  if (document.documentElement.getAttribute('data-theme') == 'dark') {
    // 查找标题元素 - 修复了选择器问题
    var siteNameElem = document.querySelector('#blog-info .site-name');
    
    if (siteNameElem) {
      siteNameElem.style.textShadow = arr[idx] + " 0 0 15px";
    }
    
    // 其他元素（如果有）
    var siteTitleElem = document.getElementById("site-title");
    if (siteTitleElem) {
      siteTitleElem.style.textShadow = arr[idx] + " 0 0 15px";
    }
    
    var siteSubtitleElem = document.getElementById("site-subtitle");
    if (siteSubtitleElem) {
      siteSubtitleElem.style.textShadow = arr[idx] + " 0 0 10px";
    }
    
    var postInfoElem = document.getElementById("post-info");
    if (postInfoElem) {
      postInfoElem.style.textShadow = arr[idx] + " 0 0 5px";
    }
    
    // 作者信息（如果有）
    try {
      var authorNameElem = document.getElementsByClassName("author-info__name")[0];
      var authorDescElem = document.getElementsByClassName("author-info__description")[0];
      if (authorNameElem) authorNameElem.style.textShadow = arr[idx] + " 0 0 12px";
      if (authorDescElem) authorDescElem.style.textShadow = arr[idx] + " 0 0 12px";
    } catch (e) {
      // 忽略错误
    }
    
    idx++;
    if (idx == 8) {
      idx = 0;
    }
  } else {
    // 白天模式恢复默认
    var siteNameElem = document.querySelector('#blog-info .site-name');
    if (siteNameElem) {
      siteNameElem.style.textShadow = "#1e1e1ee0 1px 1px 1px";
    }
    
    var siteTitleElem = document.getElementById("site-title");
    if (siteTitleElem) {
      siteTitleElem.style.textShadow = "#1e1e1ee0 1px 1px 1px";
    }
    
    var siteSubtitleElem = document.getElementById("site-subtitle");
    if (siteSubtitleElem) {
      siteSubtitleElem.style.textShadow = "#1e1e1ee0 1px 1px 1px";
    }
    
    var postInfoElem = document.getElementById("post-info");
    if (postInfoElem) {
      postInfoElem.style.textShadow = "#1e1e1ee0 1px 1px 1px";
    }
    
    try {
      var authorNameElem = document.getElementsByClassName("author-info__name")[0];
      var authorDescElem = document.getElementsByClassName("author-info__description")[0];
      if (authorNameElem) authorNameElem.style.textShadow = "";
      if (authorDescElem) authorDescElem.style.textShadow = "";
    } catch (e) {
      // 忽略错误
    }
  }
}

// 确保DOM完全加载后再执行
document.addEventListener('DOMContentLoaded', function() {
  // 初始检查一次
  changeColor();
  // 开启计时器
  setInterval(changeColor, 1200);
});