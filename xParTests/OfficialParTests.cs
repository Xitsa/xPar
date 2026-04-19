using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using xParLib;

namespace xParTests
{
    /// <summary>
    /// Тесты, портированные из официального test-par (Par 1.53.0).
    /// Каждый тест соответствует одному вызову test_par из оригинального скрипта.
    /// </summary>
    public class OfficialParTests
    {
        // ============================================================
        // Helpers
        // ============================================================

        private static ParOptions ParseOptions(params string[] args)
        {
            var bodyChars = Charset.Parse("");
            var protectChars = Charset.Parse("");
            var quoteChars = Charset.Parse("> ");
            var whiteChars = Charset.Parse(" \f\n\r\t\v");
            var terminalChars = Charset.Parse(".?!:");

            return ParOptions.Parse(args, bodyChars, protectChars, quoteChars, whiteChars, terminalChars);
        }

        private static IReadOnlyList<string> T(params string[] lines)
        {
            return lines;
        }

        private static IReadOnlyList<string> Transform(IReadOnlyList<string> lines, params string[] args)
        {
            var transformer = new StringTransformer();
            var options = ParseOptions(args);
            return transformer.Transform(lines, options);
        }

        private static void AssertLines(IReadOnlyList<string> actual, params string[] expected)
        {
            Assert.Equal(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        // ============================================================
        // From the Examples section of par.doc
        // ============================================================

        [Fact]
        public void Official_Preamble_Width39()
        {
            var input = T(
                "        We the people of the United States,",
                "        in order to form a more perfect union,",
                "        establish justice,",
                "        insure domestic tranquility,",
                "        provide for the common defense,",
                "        promote the general welfare,",
                "        and secure the blessing of liberty",
                "        to ourselves and our posterity,",
                "        do ordain and establish the Constitution",
                "        of the United States of America."
            );

            var result = Transform(input, "39");

            AssertLines(result,
                "        We the people of the United",
                "        States, in order to form a",
                "        more perfect union, establish",
                "        justice, insure domestic",
                "        tranquility, provide for the",
                "        common defense, promote the",
                "        general welfare, and secure",
                "        the blessing of liberty to",
                "        ourselves and our posterity,",
                "        do ordain and establish the",
                "        Constitution of the United",
                "        States of America."
            );
        }

        [Fact]
        public void Official_CommentBlock_Width59()
        {
            var input = T(
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, */",
                "        /* insure domestic tranquility, */",
                "        /* provide for the common defense, */",
                "        /* promote the general welfare, */",
                "        /* and secure the blessing of liberty */",
                "        /* to ourselves and our posterity, */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America. */"
            );

            var result = Transform(input, "59");

            AssertLines(result,
                "        /*   We the people of the United States, in      */",
                "        /* order to form a more perfect union, establish */",
                "        /* justice, insure domestic tranquility, provide */",
                "        /* for the common defense, promote the general   */",
                "        /* welfare, and secure the blessing of liberty   */",
                "        /* to ourselves and our posterity, do ordain     */",
                "        /* and establish the Constitution of the United  */",
                "        /* States of America.                            */"
            );
        }

        [Fact]
        public void Official_CommentBlock_Width59_Fit()
        {
            var input = T(
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, */",
                "        /* insure domestic tranquility, */",
                "        /* provide for the common defense, */",
                "        /* promote the general welfare, */",
                "        /* and secure the blessing of liberty */",
                "        /* to ourselves and our posterity, */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America. */"
            );

            var result = Transform(input, "59f");

            AssertLines(result,
                "        /*   We the people of the United States,  */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, insure domestic     */",
                "        /* tranquility, provide for the common    */",
                "        /* defense, promote the general welfare,  */",
                "        /* and secure the blessing of liberty to  */",
                "        /* ourselves and our posterity, do ordain */",
                "        /* and establish the Constitution of the  */",
                "        /* United States of America.              */"
            );
        }

        [Fact]
        public void Official_CommentBlock_Width59_Last()
        {
            var input = T(
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, */",
                "        /* insure domestic tranquility, */",
                "        /* provide for the common defense, */",
                "        /* promote the general welfare, */",
                "        /* and secure the blessing of liberty */",
                "        /* to ourselves and our posterity, */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America. */"
            );

            var result = Transform(input, "59l");

            AssertLines(result,
                "        /*   We the people of the United States, in      */",
                "        /* order to form a more perfect union, establish */",
                "        /* justice, insure domestic tranquility,         */",
                "        /* provide for the common defense, promote       */",
                "        /* the general welfare, and secure the           */",
                "        /* blessing of liberty to ourselves and our      */",
                "        /* posterity, do ordain and establish the        */",
                "        /* Constitution of the United States of America. */"
            );
        }

        [Fact]
        public void Official_CommentBlock_Width59_Last_Fit()
        {
            var input = T(
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, */",
                "        /* insure domestic tranquility, */",
                "        /* provide for the common defense, */",
                "        /* promote the general welfare, */",
                "        /* and secure the blessing of liberty */",
                "        /* to ourselves and our posterity, */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America. */"
            );

            var result = Transform(input, "59lf");

            AssertLines(result,
                "        /*   We the people of the United States,  */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, insure domestic     */",
                "        /* tranquility, provide for the common    */",
                "        /* defense, promote the general welfare,  */",
                "        /* and secure the blessing of liberty     */",
                "        /* to ourselves and our posterity, do     */",
                "        /* ordain and establish the Constitution  */",
                "        /* of the United States of America.       */"
            );
        }

        [Fact]
        public void Official_CommentBlock_Width59_Justify()
        {
            var input = T(
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, */",
                "        /* insure domestic tranquility, */",
                "        /* provide for the common defense, */",
                "        /* promote the general welfare, */",
                "        /* and secure the blessing of liberty */",
                "        /* to ourselves and our posterity, */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America. */"
            );

            var result = Transform(input, "59j");

            AssertLines(result,
                "        /*   We  the people  of  the  United States,  in */",
                "        /* order to form a more perfect union, establish */",
                "        /* justice, insure domestic tranquility, provide */",
                "        /* for the  common defense, promote  the general */",
                "        /* welfare, and  secure the blessing  of liberty */",
                "        /* to ourselves and our posterity, do ordain and */",
                "        /* establish  the  Constitution  of  the  United */",
                "        /* States of America.                            */"
            );
        }

        [Fact]
        public void Official_CommentBlock_Width59_Justify_Last()
        {
            var input = T(
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, */",
                "        /* insure domestic tranquility, */",
                "        /* provide for the common defense, */",
                "        /* promote the general welfare, */",
                "        /* and secure the blessing of liberty */",
                "        /* to ourselves and our posterity, */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America. */"
            );

            var result = Transform(input, "59jl");

            AssertLines(result,
                "        /*   We  the   people  of  the   United  States, */",
                "        /* in   order    to   form   a    more   perfect */",
                "        /* union,  establish  justice,  insure  domestic */",
                "        /* tranquility, provide for  the common defense, */",
                "        /* promote  the  general   welfare,  and  secure */",
                "        /* the  blessing  of  liberty to  ourselves  and */",
                "        /* our  posterity, do  ordain and  establish the */",
                "        /* Constitution of the United States of America. */"
            );
        }

        // ============================================================
        // Hang option
        // ============================================================

        [Fact]
        public void Official_Hang3_Width52()
        {
            var input = T(
                "        Preamble      We the people of the United States,",
                "        to the US     in order to form",
                "        Constitution  a more perfect union,",
                "                      establish justice,",
                "                      insure domestic tranquility,",
                "                      provide for the common defense,",
                "                      promote the general welfare,",
                "                      and secure the blessing of liberty",
                "                      to ourselves and our posterity,",
                "                      do ordain and establish",
                "                      the Constitution",
                "                      of the United States of America."
            );

            var result = Transform(input, "52h3");

            AssertLines(result,
                "        Preamble      We the people of the United",
                "        to the US     States, in order to form a",
                "        Constitution  more perfect union, establish",
                "                      justice, insure domestic",
                "                      tranquility, provide for the",
                "                      common defense, promote the",
                "                      general welfare, and secure",
                "                      the blessing of liberty to",
                "                      ourselves and our posterity,",
                "                      do ordain and establish the",
                "                      Constitution of the United",
                "                      States of America."
            );
        }

        // ============================================================
        // Quote handling
        // ============================================================

        [Fact]
        public void Official_Quote_Width52()
        {
            var input = T(
                "        > > We the people",
                "        > > of the United States,",
                "        > > in order to form a more perfect union,",
                "        > > establish justice,",
                "        > > ensure domestic tranquility,",
                "        > > provide for the common defense,",
                "        >",
                "        > Promote the general welfare,",
                "        > and secure the blessing of liberty",
                "        > to ourselves and our posterity,",
                "        > do ordain and establish",
                "        > the Constitution of the United States of America."
            );

            var result = Transform(input, "52q");

            AssertLines(result,
                "        > > We the people of the United States, in",
                "        > > order to form a more perfect union,",
                "        > > establish justice, ensure domestic",
                "        > > tranquility, provide for the common",
                "        > > defense,",
                "        >",
                "        > Promote the general welfare, and secure",
                "        > the blessing of liberty to ourselves and",
                "        > our posterity, do ordain and establish",
                "        > the Constitution of the United States of",
                "        > America."
            );
        }

        [Fact]
        public void Official_QuoteDiv_Width52()
        {
            var input = T(
                "        >   We the people",
                "        > of the United States,",
                "        > in order to form a more perfect union,",
                "        > establish justice,",
                "        > ensure domestic tranquility,",
                "        > provide for the common defense,",
                "        >   Promote the general welfare,",
                "        > and secure the blessing of liberty",
                "        > to ourselves and our posterity,",
                "        > do ordain and establish",
                "        > the Constitution of the United States of America."
            );

            var result = Transform(input, "52d");

            AssertLines(result,
                "        >   We the people of the United States,",
                "        > in order to form a more perfect union,",
                "        > establish justice, ensure domestic",
                "        > tranquility, provide for the common",
                "        > defense,",
                "        >   Promote the general welfare, and secure",
                "        > the blessing of liberty to ourselves and",
                "        > our posterity, do ordain and establish",
                "        > the Constitution of the United States of",
                "        > America."
            );
        }

        // ============================================================
        // Guess option
        // ============================================================

        [Fact]
        public void Official_Guess_Width50()
        {
            var input = T(
                "        I sure hope there's still room",
                "        in Dr. Jones' section of archaeology.",
                "        I've heard he's the bestest.  [sic]"
            );

            var result = Transform(input, "50g");

            AssertLines(result,
                "        I sure hope there's still room in",
                "        Dr. Jones' section of archaeology.  I've",
                "        heard he's the bestest. [sic]"
            );
        }

        [Fact]
        public void Official_GuessCap_Width50()
        {
            var input = T(
                "        I sure hope there's still room",
                "        in Dr. Jones' section of archaeology.",
                "        I've heard he's the bestest.  [sic]"
            );

            var result = Transform(input, "50gc");

            AssertLines(result,
                "        I sure hope there's still room in",
                "        Dr. Jones' section of archaeology.  I've",
                "        heard he's the bestest.  [sic]"
            );
        }

        // ============================================================
        // Body option
        // ============================================================

        [Fact]
        public void Official_Body_Width50()
        {
            var input = T(
                "        amc> The b option was added primarily to deal with",
                "        amc> this new style of quotation",
                "        amc> which became popular after Par 1.41 was released.",
                "        amc>",
                "        amc> Par still pays attention to body characters.",
                "        amc> Par should not mistake \"Par\" for part of the prefix.",
                "        amc> Par should not mistake \".\" for a suffix."
            );

            var result = Transform(input, "B=._A_a", "50bg");

            AssertLines(result,
                "        amc> The b option was added primarily to",
                "        amc> deal with this new style of quotation",
                "        amc> which became popular after Par 1.41",
                "        amc> was released.",
                "        amc>",
                "        amc> Par still pays attention to body",
                "        amc> characters.  Par should not mistake",
                "        amc> \"Par\" for part of the prefix.  Par",
                "        amc> should not mistake \".\" for a suffix."
            );
        }

        // ============================================================
        // Nested quote with custom quote chars
        // ============================================================

        [Fact]
        public void Official_NestedQuote_CustomQuoteChars()
        {
            var input = T(
                "        John writes:",
                "        : Mary writes:",
                "        : + Anastasia writes:",
                "        : + > Hi all!",
                "        : + Hi Ana!",
                "        : Hi Ana & Mary!",
                "        Please unsubscribe me from alt.hello."
            );

            var result = Transform(input, "Q+:+", "q");

            AssertLines(result,
                "        John writes:",
                "",
                "        : Mary writes:",
                "        :",
                "        : + Anastasia writes:",
                "        : +",
                "        : + > Hi all!",
                "        : +",
                "        : + Hi Ana!",
                "        :",
                "        : Hi Ana & Mary!",
                "",
                "        Please unsubscribe me from alt.hello."
            );
        }

        // ============================================================
        // Simple guess with terminal chars
        // ============================================================

        [Fact]
        public void Official_Guess_SimpleTerminal()
        {
            var input = T(
                "        One.",
                "        Two:",
                "        Three."
            );

            var result = Transform(input, "g");

            AssertLines(result,
                "        One.  Two:  Three."
            );
        }

        [Fact]
        public void Official_Guess_RemoveColonFromTerminal()
        {
            var input = T(
                "        One.",
                "        Two:",
                "        Three."
            );

            var result = Transform(input, "g", "Z-:");

            AssertLines(result,
                "        One.  Two: Three."
            );
        }

        // ============================================================
        // Repeat option (bodiless lines)
        // ============================================================

        [Fact]
        public void Official_Repeat_BodilessLines()
        {
            var input = T(
                "        /*****************************************/",
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, insure domestic    */",
                "        /* tranquility,                          */",
                "        /*                                       */",
                "        /*                                       */",
                "        /*   [ provide for the common defense, ] */",
                "        /*   [ promote the general welfare,    ] */",
                "        /*   [ and secure the blessing of liberty ] */",
                "        /*   [ to ourselves and our posterity, ] */",
                "        /*   [                                 ] */",
                "        /*                                       */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America.       */",
                "        /******************************************/"
            );

            var result = Transform(input, "42r");

            AssertLines(result,
                "        /********************************/",
                "        /*   We the people of the       */",
                "        /* United States, in order to   */",
                "        /* form a more perfect union,   */",
                "        /* establish justice, insure    */",
                "        /* domestic tranquility,        */",
                "        /*                              */",
                "        /*                              */",
                "        /*   [ provide for the common ] */",
                "        /*   [ defense, promote the   ] */",
                "        /*   [ general welfare, and   ] */",
                "        /*   [ secure the blessing of ] */",
                "        /*   [ liberty to ourselves   ] */",
                "        /*   [ and our posterity,     ] */",
                "        /*   [                        ] */",
                "        /*                              */",
                "        /* do ordain and establish the  */",
                "        /* Constitution of the United   */",
                "        /* States of America.           */",
                "        /********************************/"
            );
        }

        [Fact]
        public void Official_RepeatExpel_BodilessLines()
        {
            var input = T(
                "        /*****************************************/",
                "        /*   We the people of the United States, */",
                "        /* in order to form a more perfect union, */",
                "        /* establish justice, insure domestic    */",
                "        /* tranquility,                          */",
                "        /*                                       */",
                "        /*                                       */",
                "        /*   [ provide for the common defense, ] */",
                "        /*   [ promote the general welfare,    ] */",
                "        /*   [ and secure the blessing of liberty ] */",
                "        /*   [ to ourselves and our posterity, ] */",
                "        /*   [                                 ] */",
                "        /*                                       */",
                "        /* do ordain and establish the Constitution */",
                "        /* of the United States of America.       */",
                "        /******************************************/"
            );

            var result = Transform(input, "42re");

            AssertLines(result,
                "        /********************************/",
                "        /*   We the people of the       */",
                "        /* United States, in order to   */",
                "        /* form a more perfect union,   */",
                "        /* establish justice, insure    */",
                "        /* domestic tranquility,        */",
                "        /*                              */",
                "        /*   [ provide for the common ] */",
                "        /*   [ defense, promote the   ] */",
                "        /*   [ general welfare, and   ] */",
                "        /*   [ secure the blessing of ] */",
                "        /*   [ liberty to ourselves   ] */",
                "        /*   [ and our posterity,     ] */",
                "        /*                              */",
                "        /* do ordain and establish the  */",
                "        /* Constitution of the United   */",
                "        /* States of America.           */",
                "        /********************************/"
            );
        }

        // ============================================================
        // Quote with repeat
        // ============================================================

        [Fact]
        public void Official_QuoteRepeat_NestedLevels()
        {
            var input = T(
                "> one",
                ">> two",
                ">>> three",
                ">>>> four",
                ">>>>> five"
            );

            var result = Transform(input, "Q=>", "qr");

            AssertLines(result,
                "> one",
                ">",
                ">> two",
                ">>",
                ">>> three",
                ">>>",
                ">>>> four",
                ">>>>",
                ">>>>> five"
            );
        }

        [Fact]
        public void Official_WildSample_01()
        {
            var input = T(
                "    /// **********************************************************************",
                "    /// * Главный класс преобразования строк.  Содержит публичный метод <see *",
                "    /// * cref=\"Transform\"/> для переформатирования входных строк абзаца. *",
                "    /// **********************************************************************"
            );

            var result = Transform(input, "78jre");

            AssertLines(result,
                "    /// **********************************************************************",
                "    /// * Главный класс преобразования строк.  Содержит публичный метод <see *",
                "    /// * cref=\"Transform\"/> для переформатирования входных строк абзаца.    *",
                "    /// **********************************************************************"
            );
        }

        [Fact]
        public void Official_WildSample_02()
        {
            var input = T(
                "",
                "",
                "`par` долго шёл рядом со мной по жизни, ещё начиная",
                "с `MS DOS` и далее в `Windows`. Но постепенно в мире",
                "распространялся `Unicode` и мультибайтные кодировки. С",
                "кодировками можно было бороться, переключая кодировку файла",
                "на время в однобайтную, но с распространением символов,",
                "которые занимали несколько кодовых позиций в строке, `par`",
                "всё более переставал справляться.",
                "",
                "",
                "Патчи Jerome Pouiller’а мне не удалось запустить под",
                "`Windows`, поэтому постепенно перестал эту утилиту",
                "использовать, хоть и всё время её мне не хватало. Я",
                "несколько порывался портировать её на другие языки с",
                "поддержкой `Unicode`, но сил не хватало."
            );

            var result = Transform(input, "78jre");

            AssertLines(result,
                "`par` долго  шёл рядом  со мной по  жизни, ещё  начиная с `MS  DOS` и  далее в",
                "`Windows`.  Но постепенно  в  мире распространялся  `Unicode` и  мультибайтные",
                "кодировки.  С  кодировками можно  было  бороться,  переключая кодировку  файла",
                "на  время в  однобайтную,  но с  распространением  символов, которые  занимали",
                "несколько кодовых позиций в строке, `par` всё более переставал справляться.",
                "",
                "Патчи  Jerome  Pouiller’а мне  не  удалось  запустить под  `Windows`,  поэтому",
                "постепенно  перестал эту  утилиту использовать,  хоть и  всё время  её мне  не",
                "хватало. Я  несколько порывался  портировать её на  другие языки  с поддержкой",
                "`Unicode`, но сил не хватало."
            );
        }
    }
}
