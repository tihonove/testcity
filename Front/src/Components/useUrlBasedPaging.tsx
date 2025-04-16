import * as React from "react";
import { useSearchParamAsState } from "../Utils";

/**
 * Удобная функция чтобы в урле была пагинация и при этом в урле она 1-based, а сюда даёт 0-based
 */
export function useUrlBasedPaging(): [number, (page: number) => void] {
    const [pageRaw, setPage] = useSearchParamAsState("page");
    const page = React.useMemo(() => (isNaN(Number(pageRaw ?? "1")) ? 0 : Number(pageRaw ?? "1")), [pageRaw]);
    return [
        page - 1,
        x => {
            setPage((x + 1).toString());
        },
    ];
}
