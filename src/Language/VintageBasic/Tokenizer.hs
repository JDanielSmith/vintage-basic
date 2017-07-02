-- | Finds and tokenizes BASIC keywords, in preparation for parsing. This allows
-- keywords to be read even if there are no spaces around them. Even though
-- the standard disallows it, many BASIC implementations allowed this to save
-- memory or screen real estate. The down side is that longer variable names
-- are not practical, since they might contain keywords.

module Language.VintageBasic.Tokenizer
    (Token(..),TokenizedLine,isDataTok,isRemTok,charTokTest,taggedCharToksToString,isStringTok,
     isBuiltinTok,taggedTokensP,tokenP,printToken) where

import Data.Char(toUpper)
import Language.VintageBasic.Builtins(Builtin,builtinToStrAssoc)
import Language.VintageBasic.LexCommon
import Text.ParserCombinators.Parsec

type TokenizedLine = Tagged [Tagged Token]

-- | Parses one or more whitespace characters, producing a space token.
spaceTokP :: Parser Token
spaceTokP = whiteSpaceChar >> whiteSpace >> return SpaceTok

data Token = StringTok { getStringTokString :: String } | RemTok { getRemTokString :: String }
           | DataTok { getDataTokString :: String }
           | CommaTok | ColonTok | SemiTok | LParenTok | RParenTok
           | DollarTok | PercentTok
           | EqTok | NETok | LETok | LTTok | GETok | GTTok
           | PlusTok | MinusTok | MulTok | DivTok | PowTok
           | AndTok | OrTok | NotTok
           | BuiltinTok Builtin
           | LetTok | DimTok | OnTok | GoTok | SubTok | ReturnTok
           | IfTok | ThenTok | ForTok | ToTok | StepTok | NextTok
           | PrintTok | InputTok | RandomizeTok | ReadTok | RestoreTok
           | DefTok | FnTok | EndTok | StopTok
           | SpaceTok | DotTok | CharTok { getCharTokChar :: Char }
             deriving (Eq,Show)

keyword :: String -> Parser String
keyword s = try (string s) <?> ("keyword " ++ s)

stringTokP :: Parser Token
-- no special escape chars allowed
stringTokP =
    do _ <- char '"'
       s <- manyTill anyChar (char '"')
       whiteSpace
       return (StringTok s)

isStringTok :: Token -> Bool
isStringTok (StringTok _) = True
isStringTok _ = False

remTokP :: Parser Token
remTokP = do _ <- keyword "REM"
             s <- many anyChar
             return (RemTok s)

isRemTok :: Token -> Bool
isRemTok (RemTok _) = True
isRemTok _ = False

dataTokP :: Parser Token
dataTokP =
    do _ <- keyword "DATA"
       s <- many anyChar
       return (DataTok s)

isDataTok :: Token -> Bool
isDataTok (DataTok _) = True
isDataTok _ = False

charTokP :: Parser Token
charTokP = do c <- legalChar; return (CharTok (toUpper c))

charTokTest :: (Char -> Bool) -> Token -> Bool
charTokTest f (CharTok c) = f c
charTokTest _ _ = False

taggedCharToksToString :: [Tagged Token] -> String
taggedCharToksToString = map (getCharTokChar . getTaggedVal)

strToTokAssoc :: [(String, Token)]
strToTokAssoc =
    [
     (",",         CommaTok),
     (":",         ColonTok),
     (";",         SemiTok),
     ("(",         LParenTok),
     (")",         RParenTok),
     ("$",         DollarTok),
     ("%",         PercentTok),
     ("=",         EqTok),
     ("<>",        NETok),
     ("<=",        LETok),
     ("<",         LTTok),
     (">=",        GETok),
     (">",         GTTok),
     ("+",         PlusTok),
     ("-",         MinusTok),
     ("*",         MulTok),
     ("/",         DivTok),
     ("^",         PowTok),
     (".",         DotTok),
     ("AND",       AndTok),
     ("OR",        OrTok),
     ("NOT",       NotTok),
     ("LET",       LetTok),
     ("DIM",       DimTok),
     ("ON",        OnTok),
     ("GO",        GoTok),
     ("SUB",       SubTok),
     ("RETURN",    ReturnTok),
     ("IF",        IfTok),
     ("THEN",      ThenTok),
     ("FOR",       ForTok),
     ("TO",        ToTok),
     ("STEP",      StepTok),
     ("NEXT",      NextTok),
     ("PRINT",     PrintTok),
     ("?",         PrintTok),
     ("INPUT",     InputTok),
     ("RANDOMIZE", RandomizeTok),
     ("READ",      ReadTok),
     ("RESTORE",   RestoreTok),
     ("DEF",       DefTok),
     ("FN",        FnTok),
     ("END",       EndTok),
     ("STOP",      StopTok)
  ] ++ [(s, BuiltinTok b) | (b,s) <- builtinToStrAssoc]

tokToStrAssoc :: [(Token, String)]
tokToStrAssoc = [(t,s) | (s,t) <- strToTokAssoc]

anyTokP :: Parser Token
anyTokP = choice ([spaceTokP, stringTokP, remTokP, dataTokP]
                  ++ [do _ <- keyword s; whiteSpace; return t | (s,t) <- strToTokAssoc]
                  ++ [charTokP]) <?> "LEGAL BASIC CHARACTER"

isBuiltinTok :: Token -> Bool
isBuiltinTok (BuiltinTok _) = True
isBuiltinTok _              = False

taggedTokenP :: Parser (Tagged Token)
taggedTokenP =
    do pos <- getPosition
       tok <- anyTokP
       return (Tagged pos tok)

-- | The main parser used to read a series of tokens from a string.
taggedTokensP :: Parser [Tagged Token]
taggedTokensP =
    do toks <- many taggedTokenP
       eof <?> ""
       return toks

-- | The single-token parser used at the parser level
tokenP :: (Token -> Bool) -> GenParser (Tagged Token) () (Tagged Token)
tokenP test = token (printToken . getTaggedVal) getPosTag testTaggedToken
    where testTaggedToken (Tagged pos tok) =
            if test tok then Just (Tagged pos tok) else Nothing

-- | Prettyprint a token for error reporting or debugging.
printToken :: Token -> String
printToken tok =
    case (lookup tok tokToStrAssoc) of
        (Just s) -> s
        Nothing ->
            case tok of
                (CharTok c) -> [c]
                (DataTok s) -> "DATA" ++ s
                (RemTok s) -> "REM" ++ s
                SpaceTok -> " "
                (StringTok s) -> "\"" ++ s ++ "\""
                _ -> error "printToken: unrecognized token."
