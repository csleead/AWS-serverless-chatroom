import { Stack, StackProps } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { createWebsocketLambdas } from './websocket-lambdas';
import { createDynamoTables } from './dynamo-tables';
import { createWebsocket } from './websocket';

export class InfrastructureStack extends Stack {
  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    const tables = createDynamoTables(this);
    const wsLambdas = createWebsocketLambdas(this, tables);
    createWebsocket(this, wsLambdas);
  }
}
