// SPDX-License-Identifier: GPL-2.0-only
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Oblikovati.Exporter.Inventor.Inv
{
    /// <summary>
    /// Normalises an Inventor expression string to the Oblikovati recipe's invariant form.
    /// </summary>
    internal static class InventorExpression
    {
        /// <summary>
        /// Canonicalises an Inventor expression for the Oblikovati recipe parser: normalises the
        /// decimal separator to '.', then strips Inventor's dimensionless unit token "ul".
        /// </summary>
        public static string Canonical(string expression) =>
            StripUnitless(NormaliseDecimalSeparator(expression));

        /// <summary>
        /// Returns a sketch dimension's driving expression in a form the Oblikovati reader can bind
        /// without forming a dependency cycle.
        ///
        /// A dimension whose expression references an Inventor auto-named model parameter (a "dN"
        /// backing another sketch/feature dimension, e.g. Inventor's d2 with expression "d1") cannot
        /// be transcribed verbatim: the exporter drops the owning dN name, and the reader re-mints
        /// its own d0, d1… per sketch as it restores dimensions in order. So Inventor's second
        /// dimension (d2 = "d1") restores as the reader's "d1" whose expression is "d1" — a
        /// self-reference the reader rejects ("expression for \"d1\" forms a dependency cycle"),
        /// which blocked MainFrame.ipt / HeadShield.ipt from loading. Such an expression is collapsed
        /// to the dimension's evaluated model value in database units (cm for length, rad for angle),
        /// which the reader always resolves and which keeps geometry exact; only the (unrepresentable)
        /// dimension-to-dimension link is lost. Literals and expressions that reference only user
        /// parameters (emitted by their authored names) pass through unchanged.
        ///
        /// Example: <c>ForDimension("d1", 26.67, isAngle: false, userParameterNames)</c> ⇒ "26.67 cm".
        /// </summary>
        public static string ForDimension(
            string expression, double modelValueDb, bool isAngle, ISet<string> userParameterNames)
        {
            string canonical = Canonical(expression);
            if (!ReferencesForeignModelParameter(canonical, userParameterNames))
            {
                return canonical;
            }

            string value = modelValueDb.ToString(CultureInfo.InvariantCulture);
            return value + (isAngle ? " rad" : " cm");
        }

        // True when the expression names a model parameter (a "dN" token) that the exporter does not
        // emit — i.e. one that is not a user parameter carried into the recipe under that same name.
        // The scan is token-aware (same identifier rule as StripUnitless), so it never trips on a
        // longer name that merely contains a "dN" substring (e.g. "wood12"); and a user parameter
        // that happens to be named "d12" is emitted, so a reference to it is not foreign.
        private static bool ReferencesForeignModelParameter(string expression, ISet<string> userParameterNames)
        {
            int i = 0;
            while (i < expression.Length)
            {
                if (!IsIdentChar(expression[i]))
                {
                    i++;
                    continue;
                }
                int start = i;
                while (i < expression.Length && IsIdentChar(expression[i]))
                {
                    i++;
                }
                string token = expression.Substring(start, i - start);
                if (IsModelParameterName(token) && !userParameterNames.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }

        // An Inventor model-parameter name is 'd' followed by one or more digits (d0, d1, d15, …).
        private static bool IsModelParameterName(string token)
        {
            if (token.Length < 2 || token[0] != 'd')
            {
                return false;
            }
            for (int i = 1; i < token.Length; i++)
            {
                if (token[i] < '0' || token[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Inventor formats expression strings with the Windows regional decimal separator (e.g.
        /// "5,000 cm" on a comma-locale host), but the Oblikovati recipe parser only accepts '.'
        /// and rejects a comma with "unexpected \",\"". In every locale whose decimal separator is
        /// a comma, Inventor uses ';' as the function-argument separator, so a ',' in an expression
        /// can only be the decimal point — making this replacement unambiguous and safe. A host
        /// whose separator is already '.' needs no change. Inventor does not group-separate
        /// expression literals, so there is no thousands separator to strip.
        /// </summary>
        private static string NormaliseDecimalSeparator(string expression)
        {
            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            return separator == "." ? expression : expression.Replace(separator, ".");
        }

        /// <summary>
        /// Inventor spells its UNITLESS unit "ul" and emits it verbatim on a dimensionless literal,
        /// e.g. "15 mm / 2 ul" (the bare 2). The Oblikovati parser does not register "ul", so it
        /// leaves the token dangling and rejects the expression with "unexpected \"ul\"" — a bare
        /// number with no unit token already parses as dimensionless (see model/param/expr_parser.go).
        /// So the canonical form is the number alone: "15 mm / 2 ul" → "15 mm / 2".
        ///
        /// The scan is token-aware — it removes only a whole "ul" identifier token, never a substring
        /// of a longer name — so a user parameter such as "ul_count" or "foul" is left intact.
        /// ("ul" is Inventor's reserved unit designation and cannot itself be a parameter name.)
        /// </summary>
        private static string StripUnitless(string expression)
        {
            var sb = new StringBuilder(expression.Length);
            int i = 0;
            while (i < expression.Length)
            {
                if (!IsIdentChar(expression[i]))
                {
                    sb.Append(expression[i]);
                    i++;
                    continue;
                }
                int start = i;
                while (i < expression.Length && IsIdentChar(expression[i]))
                {
                    i++;
                }
                if (string.CompareOrdinal(expression, start, "ul", 0, 2) == 0 && i - start == 2)
                {
                    TrimTrailingSpace(sb);
                    continue;
                }
                sb.Append(expression, start, i - start);
            }
            return sb.ToString();
        }

        // Identifier bytes mirror the reader's lexer (model/param/expr_token.go): ASCII letters,
        // digits, '_', and any non-ASCII char (so a unit symbol like "µm" stays one token). Digits
        // are included so a run is only "ul" when it is exactly the standalone unit token.
        private static bool IsIdentChar(char c) =>
            c == '_' || c > 127 || (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        // Drops the single separating space left before a removed "ul" (e.g. "2 ul" → "2 " → "2").
        private static void TrimTrailingSpace(StringBuilder sb)
        {
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
            {
                sb.Length--;
            }
        }
    }
}
