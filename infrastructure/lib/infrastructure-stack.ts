import { Stack, StackProps } from 'aws-cdk-lib';
import { Construct } from 'constructs';
import { createWebsocketLambdas } from './websocket-lambdas';
import { createDynamoTables } from './dynamo-tables';
import { createWebsocket, Websocket } from './websocket';
import { Function } from 'aws-cdk-lib/aws-lambda';
import { createDynamoDbStreamLambda } from './dynamodb-stream-lambdas';

export class InfrastructureStack extends Stack {
  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    const tables = createDynamoTables(this);
    const wsLambdas = createWebsocketLambdas(this, tables);
    const webSocket = createWebsocket(this, wsLambdas);
    const dynamoStreamLambda = createDynamoDbStreamLambda(this, tables);

    this.setLambdasEnvVariables([
      wsLambdas.connect,
      wsLambdas.disconnect,
      wsLambdas.default,
      wsLambdas.createChannel,
      wsLambdas.joinChannel,
      wsLambdas.leaveChannel,
      wsLambdas.listChannels,
      wsLambdas.sendMessage,
      wsLambdas.getConnectionInfo,
      wsLambdas.fetchMessages,
      dynamoStreamLambda
    ], webSocket);
  }

  setLambdasEnvVariables(lambdas: Function[], webSocket: Websocket) {
    lambdas.forEach(lambda => {
      lambda.addEnvironment('WEBSOCKET_STAGE_URL', webSocket.stage.url);
    });
  }
}
