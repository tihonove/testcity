import { useSearchParams } from "react-router-dom";
import { useDebouncedCallback } from "use-debounce";
import { useEffect, useState } from "react";

export function useSearchParamAsState(
    paramName: string
): [string | undefined, (nextValue: undefined | string) => void] {
    const [searchParams, setSearchParams] = useSearchParams();
    return [
        searchParams.get(paramName) ?? undefined,
        (value: undefined | string) =>
            setSearchParams(x => {
                console.log(paramName, value);
                if (value == undefined) x.delete(paramName);
                else x.set(paramName, value);
                return x;
            }),
    ];
}

export function useSearchParamDebouncedAsState(
    paramName: string,
    timeout: number,
    defaultValue?: string
): [
    string | undefined,
    (nextValue: undefined | string) => void,
    string | undefined,
    (nextValue: undefined | string) => void,
] {
    const [searchParams, setSearchParams] = useSearchParams();
    const searchValue = searchParams.get(paramName) ?? defaultValue;
    const [value, setValue] = useState<string | undefined>(searchValue ?? defaultValue);

    useEffect(() => {
        setValue(searchValue);
    }, [searchValue]);

    const updateSearchValue = useDebouncedCallback(
        (value: string | undefined) =>
            setSearchParams(x => {
                if (value == undefined) x.delete(paramName);
                else x.set(paramName, value);
                return x;
            }),
        timeout
    );

    return [
        value ?? undefined,
        (x: string | undefined) => {
            setValue(x);
            updateSearchValue(x);
        },
        searchValue,
        x => {
            if (!x) setSearchParams();
            else setSearchParams({ [paramName]: x });
        },
    ];
}
