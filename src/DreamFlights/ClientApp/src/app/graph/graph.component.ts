import { Component, OnInit, ViewChild, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ElementRef, Renderer2 } from '@angular/core';
import { Observable } from 'rxjs';
import 'rxjs/add/observable/interval';

declare var vis: any;

@Component({
  selector: 'app-graph',
  templateUrl: './graph.component.html'
})
export class GraphComponent implements OnInit {
  @ViewChild("siteConfigNetwork")
  networkContainer: ElementRef;

  public network: any;


  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  getGraph() {
    return this.http.get<IGraph>(this.baseUrl + 'Graph',
      { observe: "body" });
  }

  getTraffic(): Observable<ITraffic[]> {
    return this.http.get<ITraffic[]>(this.baseUrl + 'Traffic',
      { observe: "body" });
  }

  ngOnInit() {

    var edges = new vis.DataSet();

    this.getGraph()
      .subscribe((data: IGraph) => {

        var displayMultiplier = 5;

        var nodes = new vis.DataSet();


        for (var i = 0; i < data.nodeCount; i++) {
          nodes.add(
            { id: i, label: data.nodes[i].address, x: data.nodes[i].x * displayMultiplier, y: data.nodes[i].y * displayMultiplier, fixed: true }
          );
        }

        for (var i = 0; i < data.edgeCount; i++) {
          edges.add(
            {
              from: data.edges[i].start, to: data.edges[i].end, length: data.edges[i].weight * displayMultiplier, id: i
            }
          );
        }

        this.loadVisTree(nodes, edges); // RENDER STANDARD NODES WITH TEXT LABEL
      });

    Observable
      .interval(1000)
      .subscribe((() => {
        return this.getTraffic()
          .subscribe((data: ITraffic[]) => {

            for (var i = 0; i < edges.length; i++) {
              var thisEdge = edges.get(i);
              thisEdge.color = {
                color: 'black',
                hover: 'black',
                highlight: 'black'
              }

              edges.update(thisEdge);
            }

            for (var i = 0; i < data.length; i++) {


              var thisEdge = edges.get({
                filter: function (item) {
                  return (item.from == data[i].from && item.to == data[i].to);
                }
              })[0];

              thisEdge.color = {
                color: 'red',
                hover: 'red',
                highlight: 'red'
              }

              edges.update(thisEdge);
            }
          });
      }));
  }

  loadVisTree(nodes, edges) {
    var options = {
      interaction: {
        hover: true,
      },
      manipulation: {
        enabled: true
      },
      edges: {
        arrows: 'to',
        color: {
          inherit: false
        }
      },
      nodes: {
        shape: 'box'
      }
    };

    var data = {
      nodes: nodes,
      edges: edges
    };
    var container = this.networkContainer.nativeElement;
    this.network = new vis.Network(container, data, options);

    this.network.on("hoverNode",
      function (params) {
        console.log('hoverNode Event:', params);
      });
    this.network.on("blurNode",
      function (params) {
        console.log('blurNode event:', params);
      });



  }
}


interface IGraph {
  nodeCount: number;
  edgeCount: number;
  edges: IEdge[];
  nodes: INode[];
}

interface IEdge {
  start: number;
  end: number;
  weight: number;
}

interface INode {
  x: number;
  y: number;
  address: string;
}

interface ITraffic {
  from: number;
  to: number;
  count: number;
}
