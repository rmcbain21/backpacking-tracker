// Shows a Gear List's slim weight bar once the Starting Pack Weight row has scrolled up under the top bar,
// and takes the backpacker back up to the full totals when they tap it.

export function watch(row, bar) {
    const appBar = document.querySelector('.mud-appbar')?.getBoundingClientRect().height ?? 48;
    const show = shown => {
        bar.classList.toggle('shown', shown);
        bar.inert = !shown;
    };
    const observer = new IntersectionObserver(([entry]) => {
        show(!entry.isIntersecting && entry.boundingClientRect.top < appBar);
    }, { rootMargin: `-${appBar}px 0px 0px 0px` });
    observer.observe(row);
    show(false);

    const backToTotals = () => {
        const smooth = !matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.scrollTo({ top: 0, behavior: smooth ? 'smooth' : 'auto' });
    };
    bar.addEventListener('click', backToTotals);

    return {
        stop: () => {
            observer.disconnect();
            bar.removeEventListener('click', backToTotals);
        },
    };
}
