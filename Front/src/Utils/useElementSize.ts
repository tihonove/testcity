import * as React from "react";

interface ElementSize {
    width: number;
    height: number;
}

export function useElementSize<T extends HTMLElement>(ref: React.RefObject<T>): ElementSize | undefined {
    const [size, setSize] = React.useState<ElementSize | undefined>(undefined);

    React.useEffect(() => {
        const updateSize = () => {
            if (ref.current) {
                setSize({
                    width: ref.current.clientWidth,
                    height: ref.current.clientHeight,
                });
            }
        };

        updateSize();
        window.addEventListener("resize", updateSize);
        return () => {
            window.removeEventListener("resize", updateSize);
        };
    }, [ref]);

    return size;
}
