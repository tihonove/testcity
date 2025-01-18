declare module "@carrotsearch/foamtree" {
    interface FoamTreeOptions {
        element: HTMLElement | null;
        dataObject: {
            groups: FoamTreeGroup[];
        };
        layout?: string;
        stacking?: string;
        groupMinDiameter?: number;
        maxGroupLevelsDrawn?: number;
        maxGroupLabelLevelsDrawn?: number;
        maxGroupLevelsAttached?: number;
    }

    class CarrotSearchFoamTree {
        constructor(options: FoamTreeOptions);
        set(key: string, value: unknown): void;
        dispose(): void;
    }

    export = CarrotSearchFoamTree;
}
