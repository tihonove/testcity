import * as React from "react";
import { useState } from "react";
import styles from "./useDelayedTransition.module.css";

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

export function SuspenseFadingWrapper({ children, fading }: { children: React.ReactNode; fading: boolean }) {
    return (
        <div className={`${styles.suspenseFadingWrapper} ${fading ? styles.suspenseFadingWrapperFading : ""}`}>
            {children}
        </div>
    );
}
