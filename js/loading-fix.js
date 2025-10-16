document.addEventListener('DOMContentLoaded', function() {
    const catalogMagnet = document.getElementById('catalog_magnet');
    
    if (catalogMagnet) {
        // 监听磁贴内的点击事件
        catalogMagnet.addEventListener('click', function(e) {
            // 过滤只处理磁贴项目的点击
            if (e.target.closest('.magnet-item')) {
                // 短暂延迟后重新启用动画
                setTimeout(function() {
                    const loadingElements = document.querySelectorAll('.loading-animation');
                    loadingElements.forEach(function(el) {
                        // 先移除动画再重新添加
                        el.style.animation = 'none';
                        void el.offsetWidth; // 触发重绘
                        el.style.animation = null;
                    });
                }, 100);
            }
        });
    }
});