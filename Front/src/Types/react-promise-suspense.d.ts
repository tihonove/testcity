declare module "react-promise-suspense" {
    declare function usePromise<TR, TArgs, Extra extends unknown[]>(
        promise: (...args: TArgs) => Promise<TR>,
        inputs: [...TArgs, ...Extra],
        lifespan?: number
    ): TR;
    export = usePromise;
}
