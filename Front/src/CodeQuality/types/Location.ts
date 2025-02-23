export type Location =
    | {
          path: string;
          lines:
              | {
                    begin: number;
                    end: number;
                }
              | undefined;
      }
    | {
          path: string;
          positions:
              | {
                    begin?: {
                        line: number;
                        column: number;
                    };
                    end?: {
                        line: number;
                        column: number;
                    };
                }
              | undefined;
      };
