import { WebSocketApi, WebSocketStage } from '@aws-cdk/aws-apigatewayv2-alpha';
import { WebSocketLambdaIntegration } from '@aws-cdk/aws-apigatewayv2-integrations-alpha';
import { Construct } from "constructs";
import { WebsocketLambdas } from "./websocket-lambdas";

export function createWebsocket(scope: Construct, lambdas: WebsocketLambdas) {
  const wsApi = new WebSocketApi(scope, 'ServerlessChatroomApi', {
    connectRouteOptions: { integration: new WebSocketLambdaIntegration('ConnectIntegration', lambdas.connect) },
    disconnectRouteOptions: { integration: new WebSocketLambdaIntegration('DisconnectIntegration', lambdas.disconnect) },
    defaultRouteOptions: { integration: new WebSocketLambdaIntegration('DefaultIntegration', lambdas.default) },
  });

  new WebSocketStage(scope, 'WebSocketStage', {
    webSocketApi: wsApi,
    stageName: 'prod',
    autoDeploy: true,
  });
}