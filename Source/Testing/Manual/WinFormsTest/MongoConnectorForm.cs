/*<FILE_LICENSE>
* NFX (.NET Framework Extension) Unistack Library
* Copyright 2003-2018 Agnicore Inc. portions ITAdapter Corp. Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using NFX;
using NFX.Serialization.BSON;
using NFX.Serialization.JSON;
using NFX.DataAccess.MongoDB.Connector;


namespace WinFormsTest
{
  public partial class MongoConnectorForm : Form
  {
    public MongoConnectorForm()
    {
      InitializeComponent();
    }

    private void btnInsert_Click(object sender, EventArgs e)
    {
      const int CNT = 50000;

      var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

      var sw = new System.Diagnostics.Stopwatch();

      for(var i=0; i<CNT; i++)
      {
        if (i==1) sw.Start();
        c.Insert( new BSONDocument()
                       .Set( new BSONInt32Element("_id", i) )
                       .Set( new BSONStringElement("name", "Kozlov-"+i.ToString()) )
                       .Set( new BSONInt32Element("age", i&0xf) )
                );
      }

      var el = sw.ElapsedMilliseconds;

      Text = "Inserted {0} in {1} ms at {2}ops/sec".Args(CNT, el, CNT / (el / 1000d));
    }

    private void btnFetch_Click(object sender, EventArgs e)
    {
      var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

      var sw = new System.Diagnostics.Stopwatch();

      var cnt = 0;
      var query = new Query();
      BSONDocument lastDoc = null;
      using(var cursor = c.Find(query))
        foreach(var doc in cursor)
        {
          cnt++;
          if (cnt==1) sw.Start();
          lastDoc = doc;
        //  if (cnt>3) break;
        }

      var el = sw.ElapsedMilliseconds;

      Text = "Fetched {0} in {1} ms at {2}ops/sec".Args(cnt, el, cnt / (el / 1000d));
      MessageBox.Show(lastDoc.ToString());
    }


     private void btnQuery_Click(object sender, EventArgs e)
     {
       var qry = new Query("{'$and': [{lastName: {'$ge': '$$ln'}}, {firstName: {'$ge': '$$fn'}}], kaka: true}", true,
                               new TemplateArg("fn", BSONElementType.String, "Dima"),
                               new TemplateArg("ln", BSONElementType.String, "Kozlevich")
                          );

       MessageBox.Show( qry.ToString() );
     }

     private void btnUpdate_Click(object sender, EventArgs e)
     {
         const int CNT = 5000;

      var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

      var sw = new System.Diagnostics.Stopwatch();

      var total = 0;
      for(var i=0; i<CNT; i++)
      {
        if (i==1) sw.Start();
        var result = c.Update( new UpdateEntry(
                    new Query("{'_id': '$$ID'}", true, new TemplateArg("ID", BSONElementType.Int32, i)),
                    new Update("{'$inc': {'age': '$$INCREMENT'}}", true, new TemplateArg("INCREMENT", BSONElementType.Int32, 1)),
                    multi: false,
                    upsert: false
                  ));
        total += result.TotalDocumentsAffected;
      }

      var el = sw.ElapsedMilliseconds;

      Text = "Did {0}, Updated {1} in {2} ms at {3}ops/sec".Args(CNT, total, el, CNT / (el / 1000d));
     }

     private void btnSave_Click(object sender, EventArgs e)
     {
          var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

          var query =  new Query("{'_id': '$$ID'}", true, new TemplateArg("ID", BSONElementType.Int32, 7));

          var doc = c.FindOne(query);

          doc["age"].ObjectValue = 777;

          var result = c.Save(doc);

          Text = "Affected: {0} Updated: {1}".Args(result.TotalDocumentsAffected, result.TotalDocumentsUpdatedAffected);
     }

     private void btnOpenCursors_Click(object sender, EventArgs e)
     {
        var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

        var query = new Query();
        for(var i=0; i<10; i++)
          c.Find(query);

     }

     private void btnListCollections_Click(object sender, EventArgs e)
     {
       var colls = MongoClient.Instance.DefaultLocalServer["db1"].GetCollectionNames();

       MessageBox.Show( string.Join("\n\r", colls) );
     }

     private void btnFetchOrderBy_Click(object sender, EventArgs e)
     {
        var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

        var query =  new Query(
        @"{
           '$query': {'_id': {'$lte': '$$ID'}},
           '$orderby': {'age': '$$ORD'}
          }",
           true,

         new TemplateArg("ID", BSONElementType.Int32, 197),
         new TemplateArg("ORD", BSONElementType.Int32, -1)
        );

        var data = c.FindAndFetchAll(query, cursorFetchLimit: 4);
        MessageBox.Show( "Got {0} \n=======================\n {1}".Args(data.Count, data.ToJSON(JSONWritingOptions.PrettyPrint)) );
     }

     private void btnCreateIndex_Click(object sender, EventArgs e)
     {
        var db = MongoClient.Instance.DefaultLocalServer["db1"];


        var idx = new BSONDocument(@"
          {
           createIndexes: 't1',
            indexes: [
              {
                key: {name: 1},
                name: 'idxT1_Name',
                unique: false
              },
              {
                key: {age: -11},
                name: 'idxT1_Age',
                unique: false
              }
            ]
          }


        ",false);

        MessageBox.Show( db.RunCommand( idx ).ToString() );

     }

     private void btnListIndexes_Click(object sender, EventArgs e)
     {
       var db = MongoClient.Instance.DefaultLocalServer["db1"];


        var idx = new BSONDocument("{listIndexes: 't1'}",false);

        MessageBox.Show( db.RunCommand( idx ).ToString() );
     }

    private void btnAgregate_Click(Object sender,EventArgs e)
    {
      var sw = new System.Diagnostics.Stopwatch();

      var query = new Query(
@"
{
  'aggregate': 't1',
  'pipeline': [
     {
       '$group':
         {
           '_id': '$age',
           'totalAmount': { '$sum': '$_id' },
           'count': { '$sum': 1 }
         }
     }
   ]
}", false);

      sw.Start();
      var doc = MongoClient.Instance.DefaultLocalServer["db1"].RunCommand(query);
      var cnt = 500000;//((doc["result"] as BSONArrayElement).Value[0] as BSONDocumentElement).Value["count"].ObjectValue.AsLong();

      var el = sw.ElapsedMilliseconds;

      Text = "Fetched {0} in {1} ms at {2}ops/sec".Args(cnt, el, cnt / (el / 1000d));
      MessageBox.Show(doc.ToString());

    }

    private void btnInserComplexObjects_Click(Object sender,EventArgs e)
    {
      const int CNT = 10555000;

      var c = MongoClient.Instance.DefaultLocalServer["db1"]["t1"];

      var sw = new System.Diagnostics.Stopwatch();

       //for(var i=0; i < CNT; i++)
      Parallel.For(0 ,CNT, (i) =>
      {
        if (i==1) sw.Start();

        var k = i + 1;
        double total;
        var lines = getRndOrderLines(out total);

        var v = ExternalRandomGenerator.Instance.NextScaledRandomInteger(1, 23);
        var vendorId = (v == 0 ? 11 : v) + 123456789123000;

        c.Insert( new BSONDocument()
                        .Set( new BSONInt32Element("_id", k) )
                        .Set( new BSONStringElement("desc", "order-" + k) )
                        .Set( new BSONStringElement("customer", "Albert-" + k) )
                        .Set( new BSONStringElement("vendor", "John-" + v) )
                        .Set( new BSONInt64Element("vid", vendorId) )
                        .Set( new BSONDoubleElement("total", total) )
                        .Set( lines )
                );
      });

      var el = sw.ElapsedMilliseconds;

      Text = "Inserted {0} in {1} ms at {2}ops/sec".Args(CNT, el, CNT / (el / 1000d));
    }


    private BSONArrayElement getRndOrderLines(out double total)
    {
      var linesCnt = ExternalRandomGenerator.Instance.NextScaledRandomInteger(2, 7);
      var arr = new BSONDocumentElement[linesCnt];
      total = 0;
      for(var i = 0; i < linesCnt; i ++)
      {
        var k = i + 1;
        var e = new BSONDocument();
        e.Set( new BSONInt32Element("lid", k) );
        e.Set( new BSONStringElement("desc", "odr-ln-" + k) );
        e.Set( new BSONStringElement("name", "product-" + k) );

        var amt =ExternalRandomGenerator.Instance.NextScaledRandomDouble(35, 749);
        total += amt;
        e.Set( new BSONDoubleElement("amnt", amt) );

        arr[i] = new BSONDocumentElement(e);
      }

      return new BSONArrayElement("lines", arr);
    }

    private void btnAggComplex_Click(Object sender,EventArgs e)
    {
      var sw = new System.Diagnostics.Stopwatch();

      var query = new Query(tbQuery.Text, false);

      sw.Start();
      var doc = MongoClient.Instance.DefaultLocalServer["db1"].RunCommand(query);

      var el = sw.ElapsedMilliseconds;

      var cnt = 0;
      var arr = doc["result"] as BSONArrayElement;
      for(var i = 0; i < arr.Value.Length; i++)
      {
        cnt += (arr.Value[i] as BSONDocumentElement).Value["count"].ObjectValue.AsInt();
      }

      Text = "Fetched {0} in {1} ms at {2}ops/sec".Args(cnt, el, cnt / (el / 1000d));
      MessageBox.Show(doc.ToString());
    }
  }
}