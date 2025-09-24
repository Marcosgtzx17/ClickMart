(function () {
    function initStar(container) {
        const inputId = container.getAttribute('data-input-id');
        const input = document.getElementById(inputId);
        if (!input) return;

        const stars = Array.from(container.querySelectorAll('.star'));
        const clamp = (v) => Math.max(1, Math.min(5, v));

        function render(val) {
            const rating = Number(val) || 0;
            stars.forEach(s => {
                const v = Number(s.dataset.value);
                s.classList.toggle('active', v <= rating);
                s.setAttribute('aria-checked', v === rating ? 'true' : 'false');
                if (v === rating) s.setAttribute('tabindex', '0'); else s.setAttribute('tabindex', '-1');
            });
        }

        function set(val, focusIndex = null) {
            const v = clamp(val);
            input.value = v;
            render(v);
            if (focusIndex != null && stars[focusIndex]) stars[focusIndex].focus();
        }

        // Estado inicial desde el hidden (server-side)
        const initial = Number(input.value || 0) || 5; // fallback 5
        set(initial);

        // Eventos
        stars.forEach((star, idx) => {
            star.addEventListener('click', () => set(Number(star.dataset.value)));
            star.addEventListener('mouseenter', () => {
                const v = Number(star.dataset.value);
                stars.forEach(s => s.classList.toggle('hover', Number(s.dataset.value) <= v));
            });
            star.addEventListener('mouseleave', () => {
                stars.forEach(s => s.classList.remove('hover'));
            });
            star.addEventListener('keydown', (e) => {
                const current = Number(input.value || initial) || 5;
                if (e.key === 'ArrowRight' || e.key === 'ArrowUp') {
                    e.preventDefault();
                    set(current + 1, Math.min(4, idx + 1));
                } else if (e.key === 'ArrowLeft' || e.key === 'ArrowDown') {
                    e.preventDefault();
                    set(current - 1, Math.max(0, idx - 1));
                } else if (e.key === ' ' || e.key === 'Enter') {
                    e.preventDefault();
                    set(Number(star.dataset.value), idx);
                }
            });
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('.star-input').forEach(initStar);
    });
})();