import {useSearchParams} from "react-router-dom";

export function useSearchParamAsState(paramName: string): [string | undefined, (nextValue: undefined | string) => void] {
    let [searchParams, setSearchParams] = useSearchParams();
    return [
        searchParams.get(paramName) ?? undefined,
        (value: undefined | string) => setSearchParams(x => {
            if (value == undefined)
                x.delete(paramName)
            else
                x.set(paramName, value);
            return x;
        })
    ]
}