import * as React from "react";
import { useParams } from "react-router-dom";
import { RunStatus } from "./RunStatus";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import CarrotSearchFoamTree from "@carrotsearch/foamtree";
import { useMemo } from "react";

interface TestTreeItem {
    name: string;
    duration: number;
    children: Map<string, TestTreeItem>;
}

export function TestsTreeMapChart(): React.JSX.Element {
    const { jobId, jobRunId } = useParams();
    const client = useClickhouseClient();

    const allTests1 = client.useData2<[string, number]>(
        `SELECT TestId, Duration FROM TestRunsByRun WHERE JobId = '${jobId}' AND JobRunId = '${jobRunId}'`,
        [jobId, jobRunId, "all test runs 2"]
    );

    const allTests = useMemo(
        () => allTests1.map(x => [x[0].split(": ").slice(1).join(": "), x[1]] as const),
        [allTests1]
    );

    const root = useMemo(() => {
        const root: Map<string, TestTreeItem> = new Map();
        for (const test of allTests) {
            const splitName = splitTestName(test[0]);

            let treeNode: undefined | TestTreeItem;
            const treeNodeStack: TestTreeItem[] = [];
            for (const nameToken of splitName) {
                const items = treeNode?.children ?? root;
                treeNode =
                    items.get(nameToken) ??
                    items.set(nameToken, { name: nameToken, duration: 0, children: new Map() }).get(nameToken);
                if (treeNode) treeNodeStack.push(treeNode);
            }
            if (treeNode) treeNode.name += " " + test[1] + "ms";
            let parentNode: TestTreeItem | undefined;
            while ((parentNode = treeNodeStack.pop())) parentNode.duration += test[1];
        }
        return root;
    }, [allTests]);

    const groups: FoamTreeGroup[] = useMemo(() => {
        return [...root.values()].map(x => nodeToGroup(x));
    }, [root]);

    return <FoamTree groups={groups} />;
}

function nodeToGroup(node: TestTreeItem): FoamTreeGroup {
    return {
        label: node.name,
        weight: node.duration,
        groups: [...node.children.values()].map(nodeToGroup),
    };
}

function splitTestName(testId: string): string[] {
    const regex =
        /(?:(["'])(\\.|(?!\1)[^\\])*\1|\[(?:(["'])(\\.|(?!\2)[^\\])*\2|[^\]])*\]|\((?:(["'])(\\.|(?!\3)[^\\])*\3|[^)])*\)|[^.:])+/g;
    const parts = testId.match(regex);
    return [...parts.map(x => x.trim())];
}

interface FoamTreeGroup {
    label: string;
    weight?: number;
    groups?: FoamTreeGroup[];
}

interface FoamTreeProps {
    groups: FoamTreeGroup[];
}

class FoamTree extends React.Component<FoamTreeProps> {
    componentDidMount() {
        this.foamtree = new CarrotSearchFoamTree({
            element: this.element,
            dataObject: {
                groups: this.props.groups,
            },
            layout: "squarified",
            stacking: "flattened",
            groupMinDiameter: 0,
            maxGroupLevelsDrawn: 20,
            maxGroupLabelLevelsDrawn: 20,
            maxGroupLevelsAttached: 20,
        });
    }

    componentDidUpdate() {
        if (this.props.groups !== this.foamtree.get("dataObject").groups) {
            this.foamtree.set("dataObject", {
                groups: this.props.groups,
            });
        }
    }

    componentWillUnmount() {
        this.foamtree.dispose();
    }

    render() {
        return <div style={{ height: "95vh", width: "95vw" }} ref={e => (this.element = e)}></div>;
    }
}
