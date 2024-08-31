import { useSearchParams } from "react-router-dom";
import { useDebouncedCallback } from "use-debounce";
import { useEffect, useState } from "react";

export function formatTestDuration(seconds: string): string {
    let sec = Number(seconds);
    return new Date(sec * 1000).toISOString().slice(11, 19)
        .replace(/(\d{2}):(\d{2}):(\d{2})/, "$1h $2m $3s")
        .replace("00h 00m ", "")
        .replace("00h ", "");
}

export function formatTestCounts(total: string, passed: string, ignored: string, failed: string): string {
    let out = "Tests "
    if (failed !== '0') out += `failed: ${failed} `
    if (passed !== '0') out += `passed: ${passed} `
    if (ignored !== '0') out += `ignored: ${ignored} `
    // out += `total: ${total}`
    return out.trim();
}

export function getLinkToJob(jobRunId: string, agentName: string) {
    let project = /17358/.test(agentName)
        ? "forms"
        : /19371/.test(agentName)
            ? "extern.forms"
            : undefined;
    return project ? `https://git.skbkontur.ru/forms/${project}/-/jobs/${jobRunId}` : "https://git.skbkontur.ru/";
}

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
