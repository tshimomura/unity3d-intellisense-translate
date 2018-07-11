require 'rexml/document'
require 'nokogiri'
require 'fileutils'

require './settings'

# XMLファイルの生成先
XML_OUT_PATH = './xml/'

# 言語
LANGUAGE = 'ja'

# XML内部の表記からファイル名を生成する際の置換用
replace_hash = {
    'this' => '.Index_operator',
    'op_Equal' => '-operator_eq',
    'op_NotEqual' => '-operator_ne',
    'op_Plus' => '-operator_add',
    'op_Minus' => '-operator_subtract',
    'op_Multiply' => '-operator_multiply',
    'op_Divide' => '-operator_divide',
}

# 出力先フォルダをいったん消す
FileUtils.rm_rf(XML_OUT_PATH)

# 処理対象となるXMLファイルを検索
src_list = Array.new
['Managed', 'UnityExtensions'].each do |dir|
  Dir.glob(UNITY_DATA_PATH + dir + '/**/*.xml') do |src|
    src_list.push(src) if !src.include?('/'+LANGUAGE+'/')
  end
end

# XMLファイル内のテキストを日本語化
src_list.each do |src|
  # Unity純正の英語版XMLを読み込んでパース
  xml = REXML::Document.new(open(src))

  xml.elements.each('doc/members/member') do |member|
    # この member １つ１つが、クラスだったりメソッドだったりの定義になってる
    # summary, param, returns の３カ所のテキストを日本語に差し替える
    # 差し替え用の日本語テキストは、日本語版の HTML から Xpath を使って抜き出す

    # まずは、member に対応する、日本語版HTMLのファイル名を推測
    begin
      /.\:(UnityEngine|UnityEditor)\.(.*)/ =~ member.attributes['name']
      keyword = $2
      next if keyword == nil

      append = ''
      if keyword.include?('.')
        /([\w\.]+)\.\#*([\w]*).*?(\`*(\d*))/ =~ keyword
        clazz, field = $1, $2
        if $3.length > 0
          append = '_' + $4
        end

        if replace_hash.has_key?(field)
          filename = clazz + replace_hash[field]
        elsif field[0] == field[0].capitalize || (field == 'iOS' || field == 'tvOS')
          filename = clazz + '.' + field
        elsif keyword =~ /implop_\w+\((\w+)\)/
          filename = clazz + '-operator_' + $1
        else
          filename = clazz + '-' + field
        end
      else
        filename = keyword.tr('`', '_')
      end
      filename = filename + append + '.html'
    end
    file = UNITY_DOC_SRC_PATH + filename

    # 日本語化されたHTMLファイルがみつからない
    next unless File.exist?(file)

    # XML内のmemberの該当箇所のテキストを日本語に差し替え
    begin
      # 日本語のスクリプトリファレンスのHTMLをパース
      html = Nokogiri::HTML.parse(open(file).read, nil, 'UTF8')
      nodes = html.xpath("//div[@id='content-wrap']/div/div/div[1]/div")
      nodes.each do |node|
        # HTMLから説明,戻り値,パラメーターのいずれかのテキストを見つけたら、XMLの該当箇所を置換
        case node.xpath(".//h2").text
        when "説明"
          if member.elements['summary/para'] != nil
            member.elements['summary/para'].text = node.xpath(".//p").text
          end
        when "戻り値"
          if member.elements['returns/para'] != nil
            member.elements['returns/para'].text = node.xpath(".//p").text.strip.gsub(/[\r\n]/,'').gsub(/\s+/,' ')
          end
        when "パラメーター"
          param_nodes = node.xpath(".//table/tr")
          param_nodes.each do |param_node|
            param = param_node.xpath(".//td[@class='name lbl']").text
            desc = param_node.xpath(".//td[@class='desc']").text
            member.elements.each("param") do |x|
              if x.attributes["name"] == param
                x.text = desc
              end
            end
          end
        end
      end
    end
  end

  # 日本語に置換し終わったXMLを書き出し
  begin
    { 'MonoDevelop_or_Rider/' => true, 'VSCode/' => false}.each do |out, lang_dir|
      out_path = File.dirname(XML_OUT_PATH + out + src.sub(UNITY_DATA_PATH, '')) + '/'
      out_path += LANGUAGE + '/' if lang_dir
      out_file = out_path + File.basename(src)

      FileUtils.mkdir_p(out_path) if !Dir.exists?(out_path)
      File.open(out_file, 'w') do |file|
        file.write(xml)
      end
      File.open(XML_OUT_PATH + out + 'translate-xml.txt', 'a') do |file|
        file.write(out_file.sub(XML_OUT_PATH + out, '') + "\n")
      end
    end
  end
end

