import { backendCode } from './constants';
import { Construct } from "constructs";
import { Function, Runtime } from 'aws-cdk-lib/aws-lambda';
import { DynamoTables } from './dynamo-tables';

export function createDynamoDbStreamLambda(scope: Construct, tables: DynamoTables): Function {
  const lambda = new Function(scope, 'DynamoDBStreamLambda', {
    runtime: Runtime.DOTNET_6,
    handler: 'AwsServerlessChatroom::AwsServerlessChatroom.Function::OnNewMessages',
    code: backendCode,
  });

  tables.messagesTable.grantStreamRead(lambda);

  return lambda;
}