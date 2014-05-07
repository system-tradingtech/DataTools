#DataTools
=========

##MarketDataDNA
* version 0.1

###条件
* Excel 2010 32bit/64bitで動作確認済み
* その他のバージョンでの動作報告していただけると嬉しいです

###機能
* アドインを追加することで市場データをリアルタイム更新出来るようになる
  * 例）GetData("データソース名","銘柄コード","フィールド名")

データソース名
* YahooJP: http://finance.yahoo.co.jp/
* YahooUS: http://finance.yahoo.com/

フィールド名一覧
* LAST: 直近約定値 (YahooJP)
* OPEN: 始値 (YahooJP,YahooUS)
* HIGH: 高値 (YahooJP)
* LOW: 安値 (YahooJP)
* ASK: 売気配値 (YahooJPにて別途契約,YahooUS)
* BID: 買気配値 (YahooJPにて別途契約,YahooUS)

###ダウンロード
* このプラグインはGitHubにて公開されています。
    * https://github.com/system-tradingtech/DataTools/downloads

###インストール
* 任意のフォルダにビルドファルダの内容をコピーする。
* Excel上で、オプション->アドイン->設定->参照から先ほど作成したフォルダ内の"ExcelDNA.xll"または"ExcelDNA64.xll"を選択する。

###注意点
データは現在、
* Yahooファイナンス (JP)
* YahooFinance (US)

からスクレイピングにてデータ取得をしているので頻繁なアクセスとなる設定にご注意下さい。

###ライセンス
* MIT License (http://www.opensource.org/licenses/mit-license.php)

###コピーライト
* Copyright 2014, system-tradingtech.com([@sys-tradingtech](https://twitter.com/sys_tradingtech "twitter:@sys-tradingtech")).