import { Code } from "aws-cdk-lib/aws-lambda";

export const backendCode = Code.fromAsset('../artifacts/AwsServerlessChatroomBackend.zip');