declare module "react-promise-suspense" {
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-type-parameters
    declare function usePromise<TR, TArgs, Extra extends unknown[]>(
        promise: (...args: TArgs) => Promise<TR>,
        inputs: [...TArgs, ...Extra],
        lifespan?: number
    ): TR;
    export = usePromise;
}
