import { ReactElement } from "react";

export interface TreeNode<TDetails> {
    id: string;
    name: string;
    type: "file" | "directory";
    children?: TreeNode<TDetails>[];
    path: string;
    details?: TDetails;
}

export interface TreeViewProps<TDetails> {
    data: TreeNode<TDetails>[];
    onSelect?: (node: TreeNode<TDetails>) => void;
    renderDetails?: (details: TDetails) => ReactElement;
    selectedPath?: string;
}
