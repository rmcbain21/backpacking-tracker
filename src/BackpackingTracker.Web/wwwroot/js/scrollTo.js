// Scrolls the page so the element matching the selector is at the top, under the top bar (its CSS sets
// scroll-margin-top). Returns false when there is no such element yet, so the caller can try again after a render.

export function scrollTo(selector) {
    const element = document.querySelector(selector);
    element?.scrollIntoView({ block: 'start' });
    return !!element;
}
