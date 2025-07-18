import { useSearchParams } from "react-router-dom";
import { useDebouncedCallback } from "use-debounce";
import { useEffect, useState } from "react";
import { formatDistanceToNow, parseISO } from "date-fns";

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

export function formatRelativeTime(dateString: string): string {
    // Преобразуем строку даты/времени в объект Date и добавляем смещение часового пояса (UTC+5)
    const date = new Date(dateString.replace(" ", "T"));
    // Добавляем 5 часов чтобы скорректировать для UTC+5
    // Я лох и на серваке не сохранил вреия в UTC, поэтому приходится так делать
    date.setHours(date.getHours() + 5);
    return formatDistanceToNow(date, { addSuffix: true });
}

export function getText(
    total: string,
    passed: string,
    ignored: string,
    failed: string,
    state: string,
    info: string,
    hasCodeQualityReport: number
): string {
    if (
        hasCodeQualityReport &&
        Number(total) == 0 &&
        Number(passed) == 0 &&
        Number(ignored) == 0 &&
        Number(failed) == 0 &&
        !info
    ) {
        return "Finished";
    }
    let out = formatTestCounts(total, passed, ignored, failed);
    if (state == "Canceled") out = state;
    else if (state == "Timeouted") out = state;
    else if (state == "Failed" && out == "") out = state;
    if (!out) out = state;

    return info ? `${info}. ${out}` : out;
}

export function formatTestCounts(total: string, passed: string, ignored: string, failed: string): string {
    if (total == "0") return "";

    return (
        "Tests " +
        [
            failed != "0" ? `failed: ${failed}` : null,
            passed != "0" ? `passed: ${passed}` : null,
            ignored != "0" ? `ignored: ${ignored}` : null,
        ]
            .filter(x => x != null)
            .join("; ")
    );
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

export function useSearchParamsAsState(
    paramNames: string[],
    defaultValue?: string[]
): [
    string[] | undefined,
    (nextValue: undefined | string[] | ((prev: undefined | string[]) => undefined | string[])) => void,
] {
    const [searchParams, setSearchParams] = useSearchParams();
    const currentValue = paramNames.map(n => searchParams.get(n));

    return [
        currentValue.every(x => x != null) ? currentValue : defaultValue,
        (value: undefined | string[] | ((prev: undefined | string[]) => undefined | string[])) => {
            setSearchParams(_ => {
                // https://github.com/remix-run/react-router/issues/9757
                const params = new URLSearchParams(window.location.search);
                const prevValueRaw = paramNames.map(n => searchParams.get(n));
                const prevValue = (prevValueRaw.every(x => x != null) ? prevValueRaw : defaultValue) ?? undefined;
                const nextValue = typeof value === "function" ? value(prevValue) : value;
                if (nextValue == undefined) {
                    for (const paramName of paramNames) params.delete(paramName);
                } else {
                    for (let i = 0; i < paramNames.length; i++) params.set(paramNames[i], nextValue[i]);
                }
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
