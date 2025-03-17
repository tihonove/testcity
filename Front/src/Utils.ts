import { useSearchParams } from "react-router-dom";
import { useDebouncedCallback } from "use-debounce";
import { useEffect, useState } from "react";

export function getProjectNameById(id: string): string {
    return (
        {
            "17358": "Wolfs",
            "19371": "Forms mastering",
            "182": "Diadoc",
        }[id] ?? id
    );
}

function getHoursOffsetFromUtc(): number {
    return -(new Date().getTimezoneOffset() / 60);
}

export function getOffsetTitle(): string {
    const offsetHrs = getHoursOffsetFromUtc();
    return offsetHrs === 0
        ? "(UTC)"
        : offsetHrs > 0
          ? `(GMT+${offsetHrs.toString()})`
          : `(GMT-${offsetHrs.toString()})`;
}

export function toLocalTimeFromUtc(dateTime: string, format: "default" | "short" = "default"): string {
    return addHoursToDate(dateTime, getHoursOffsetFromUtc(), format);
}

function addHoursToDate(dateString: string, hoursToAdd: number, format: "default" | "short"): string {
    // Parse the dateString to create a Date object
    const date = new Date(dateString.replace(" ", "T"));

    // Add the specified number of hours
    date.setHours(date.getHours() + hoursToAdd);

    // Format the result to match "YYYY-MM-DD HH:mm:ss"
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0"); // Month is zero-indexed
    const day = String(date.getDate()).padStart(2, "0");
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    const seconds = String(date.getSeconds()).padStart(2, "0");

    if (format === "short") return `${day}.${month} ${hours}:${minutes}`;
    else return `${year.toString()}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}

export function formatTestDuration(seconds: string): string {
    const sec = Number(seconds);
    return new Date(sec * 1000)
        .toISOString()
        .slice(11, 19)
        .replace(/(\d{2}):(\d{2}):(\d{2})/, "$1h $2m $3s")
        .replace("00h 00m ", "")
        .replace("00h ", "");
}

export function getText(
    total: string,
    passed: string,
    ignored: string,
    failed: string,
    state: string,
    info: string
): string {
    let out = formatTestCounts(total, passed, ignored, failed);
    if (state == "Canceled") out = state;
    else if (state == "Timeouted") out = state;
    else if (state == "Failed" && out == "") out = state;

    return info ? `${info}. ${out}` : out;
}

export function formatTestCounts(total: string, passed: string, ignored: string, failed: string): string {
    if (total == "0") return "";

    let out = "Tests ";
    if (failed != "0") out += `failed: ${failed} `;
    if (passed != "0") out += `passed: ${passed} `;
    if (ignored != "0") out += `ignored: ${ignored} `;
    // out += `total: ${total}`
    return out.trim();
}

export function getLinkToJob(jobRunId: string, agentName: string) {
    const project = /17358/.test(agentName) ? "forms" : /19371/.test(agentName) ? "extern.forms" : undefined;
    return project ? `https://git.skbkontur.ru/forms/${project}/-/jobs/${jobRunId}` : "https://git.skbkontur.ru/";
}

export function useSearchParam(paramName: string, defaultValue?: string): [string | undefined] {
    const [searchParams] = useSearchParams();
    return [searchParams.get(paramName) ?? defaultValue ?? undefined];
}

export function useSearchParamAsState(
    paramName: string,
    defaultValue?: string
): [string | undefined, (nextValue: undefined | string | ((prev: undefined | string) => undefined | string)) => void] {
    const [searchParams, setSearchParams] = useSearchParams();
    return [
        searchParams.get(paramName) ?? defaultValue ?? undefined,
        (value: undefined | string | ((prev: undefined | string) => undefined | string)) => {
            setSearchParams(_ => {
                // https://github.com/remix-run/react-router/issues/9757
                const params = new URLSearchParams(window.location.search);
                const prevValue = searchParams.get(paramName) ?? defaultValue ?? undefined;
                const nextValue = typeof value === "function" ? value(prevValue) : value;
                if (nextValue == undefined) params.delete(paramName);
                else params.set(paramName, nextValue);
                return params;
            });
        },
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

    const updateSearchValue = useDebouncedCallback((value: string | undefined) => {
        setSearchParams(x => {
            if (value == undefined) x.delete(paramName);
            else x.set(paramName, value);
            return x;
        });
    }, timeout);

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

export function useFilteredValues<T>(
    value: undefined | string,
    values: T[],
    defaultValue: T | undefined
): T | undefined {
    for (const allowedValue of values) {
        if (allowedValue?.toString() == value?.toString()) {
            return allowedValue;
        }
    }
    return defaultValue;
}
