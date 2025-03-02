import * as React from "react";
import { useState } from "react";
import styled from "styled-components";

export function useDelayedTransition(): [boolean, React.TransitionStartFunction, boolean] {
    const [isPending, startTransition] = React.useTransition();
    const [isFading, setIsFading] = useState(false);
    React.useEffect(() => {
        if (isPending) {
            const timer = setTimeout(() => {
                setIsFading(true);
            }, 500); // Затемняем, если >500 мс
            return () => {
                clearTimeout(timer);
            }; // Очищаем, если загрузка завершится быстрее
        } else {
            setIsFading(false); // Возвращаем нормальную прозрачность
        }
    }, [isPending]);
    return [isPending, startTransition, isFading];
}

export const SuspenseFadingWrapper = styled.div<{ $fading: boolean }>`
    transition: opacity 0.5s ease-in-out;
    opacity: ${props => (props.$fading ? "0.3" : "1")};
`;
