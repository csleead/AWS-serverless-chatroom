import { WebSocketApi, WebSocketStage } from '@aws-cdk/aws-apigatewayv2-alpha';
import { WebSocketLambdaIntegration } from '@aws-cdk/aws-apigatewayv2-integrations-alpha';
import { StringParameter } from 'aws-cdk-lib/aws-ssm';
import { Construct } from "constructs";
import { WebsocketLambdas } from "./websocket-lambdas";

export interface Websocket {
  api: WebSocketApi;
  stage: WebSocketStage;
}

export function createWebsocket(scope: Construct, lambdas: WebsocketLambdas): Websocket {
  const wsApi = new WebSocketApi(scope, 'ServerlessChatroomApi', {
    connectRouteOptions: { integration: new WebSocketLambdaIntegration('ConnectIntegration', lambdas.connect) },
    disconnectRouteOptions: { integration: new WebSocketLambdaIntegration('DisconnectIntegration', lambdas.disconnect) },
    defaultRouteOptions: { integration: new WebSocketLambdaIntegration('DefaultIntegration', lambdas.default) },
  });

  wsApi.addRoute('getConnectionInfo', {
    integration: new WebSocketLambdaIntegration('GetConnectionInfoIntegration', lambdas.getConnectionInfo),
  });

  wsApi.addRoute('joinChannel', {
    integration: new WebSocketLambdaIntegration('JoinChannelIntegration', lambdas.joinChannel),
  });

  wsApi.addRoute('createChannel', {
    integration: new WebSocketLambdaIntegration('CreateChannelIntegration', lambdas.createChannel),
  });

  wsApi.addRoute('listChannels', {
    integration: new WebSocketLambdaIntegration('ListChannelsIntegration', lambdas.listChannels),
  });

  wsApi.addRoute('sendMessage', {
    integration: new WebSocketLambdaIntegration('SendMessageIntegration', lambdas.sendMessage),
  });

  const stage = new WebSocketStage(scope, 'WebSocketStage', {
    webSocketApi: wsApi,
    stageName: 'prod',
    autoDeploy: true,
  });

  return {
    api: wsApi,
    stage,
  };
}