-- | Parses BASIC source code, in tokenized form, to produce abstract syntax.
-- Also used at runtime to input values.

module Language.VintageBasic.Parser(exprP,statementListP) where

import Data.Char
import Text.ParserCombinators.Parsec
import Text.ParserCombinators.Parsec.Expr
import Language.VintageBasic.FloatParser
import Language.VintageBasic.LexCommon
import Language.VintageBasic.Syntax
import Language.VintageBasic.Tokenizer

-- | The number of significant letters at the start of a variable name.
varSignifLetters :: Int
varSignifLetters = 2

-- | The number of digits (following the letters) of a variable name that are significant.
varSignifDigits :: Int
varSignifDigits = 1

type TokParser = GenParser (Tagged Token) ()

skipSpace :: TokParser ()
skipSpace = skipMany $ tokenP (==SpaceTok)

lineNumP :: TokParser Int
lineNumP =
    do s <- many1 (tokenP (charTokTest isDigit) <?> "") <?> "LINE NUMBER"
       skipSpace
       return (read (map (getCharTokChar . getTaggedVal) s))

-- LITERALS

floatLitP :: TokParser Literal
floatLitP =
    do v <- floatP
       skipSpace
       return (FloatLit v)

stringLitP :: TokParser Literal
stringLitP =
    do tok <- tokenP isStringTok
       return (StringLit (getStringTokString (getTaggedVal tok)))

litP :: TokParser Literal
litP = floatLitP <|> stringLitP

-- VARIABLES

varBaseP :: TokParser String
varBaseP = do ls <- many1 (tokenP (charTokTest isAlpha))
              ds <- many (tokenP (charTokTest isDigit))
              return (taggedCharToksToString (take varSignifLetters ls
                      ++ take varSignifDigits ds))

floatVarNameP :: TokParser VarName
floatVarNameP = do
    name <- varBaseP
    return (VarName FloatType name)

intVarNameP :: TokParser VarName
intVarNameP = do
    name <- varBaseP
    _ <- tokenP (==PercentTok)
    return (VarName IntType name)

stringVarNameP :: TokParser VarName
stringVarNameP = do
    name <- varBaseP
    _ <- tokenP (==DollarTok)
    return (VarName StringType name)

-- Look for string and int vars first because of $ and % suffixes.
varNameP :: TokParser VarName
varNameP = do
    vn <- try stringVarNameP <|> try intVarNameP <|> floatVarNameP
    skipSpace
    return vn

scalarVarP :: GenParser (Tagged Token) () Var
scalarVarP = do
    vn <- varNameP
    return (ScalarVar vn)

arrVarP :: GenParser (Tagged Token) () Var
arrVarP = do
    vn <- varNameP
    xs <- argsP
    return (ArrVar vn xs)

varP :: GenParser (Tagged Token) () Var
varP = try arrVarP <|> scalarVarP

-- BUILTINS

builtinXP :: TokParser Expr
builtinXP = do
    (Tagged _ (BuiltinTok b)) <- tokenP isBuiltinTok
    xs <- argsP
    return (BuiltinX b xs)

-- EXPRESSIONS

litXP :: TokParser Expr
litXP =
    do v <- litP
       return (LitX v)

varXP :: TokParser Expr
varXP =
    do v <- varP
       return (VarX v)

argsP :: TokParser [Expr]
argsP =
    do _ <- tokenP (==LParenTok)
       xs <- sepBy exprP (tokenP (==CommaTok))
       _ <- tokenP (==RParenTok)
       return xs

fnXP :: TokParser Expr
fnXP = do
    _ <- tokenP (==FnTok)
    vn <- varNameP
    args <- argsP
    return (FnX vn args)

parenXP :: TokParser Expr
parenXP =
    do _ <- tokenP (==LParenTok)
       x <- exprP
       _ <- tokenP (==RParenTok)
       return (ParenX x)

primXP :: TokParser Expr
primXP = parenXP <|> litXP <|> builtinXP <|> fnXP <|> varXP

opTable :: OperatorTable (Tagged Token) () Expr
opTable =
    [[prefix MinusTok MinusX, prefix PlusTok id],
     [binary PowTok  (BinX PowOp) AssocRight],
     [binary MulTok  (BinX MulOp) AssocLeft, binary DivTok   (BinX DivOp) AssocLeft],
     [binary PlusTok (BinX AddOp) AssocLeft, binary MinusTok (BinX SubOp) AssocLeft],
     [binary EqTok   (BinX EqOp)  AssocLeft, binary NETok    (BinX NEOp)  AssocLeft,
      binary LTTok   (BinX LTOp)  AssocLeft, binary LETok    (BinX LEOp)  AssocLeft,
      binary GTTok   (BinX GTOp)  AssocLeft, binary GETok    (BinX GEOp)  AssocLeft],
     [prefix NotTok   NotX],
     [binary AndTok  (BinX AndOp) AssocLeft],
     [binary OrTok   (BinX OrOp)  AssocLeft]]

binary :: Token -> (Expr -> Expr -> Expr) -> Assoc -> Operator (Tagged Token) () Expr
binary tok fun assoc =
    Infix (do _ <- tokenP (==tok); return fun) assoc
prefix :: Token -> (Expr -> Expr) -> Operator (Tagged Token) () Expr
prefix tok fun =
    Prefix (do _ <- tokenP (==tok); return fun)

-- | Parses a BASIC expression from tokenized source.
exprP :: TokParser Expr
exprP = buildExpressionParser opTable primXP

-- STATEMENTS

letSP :: TokParser Statement
letSP = do
    _ <- optionally (tokenP (==LetTok))
    v <- varP
    _ <- tokenP (==EqTok)
    x <- exprP
    return (LetS v x)

gotoSP :: TokParser Statement
gotoSP = do
    _ <- try (tokenP (==GoTok) >> tokenP (==ToTok))
    n <- lineNumP
    return (GotoS n)

gosubSP :: TokParser Statement
gosubSP = do
    _ <- try (tokenP (==GoTok) >> tokenP (==SubTok))
    n <- lineNumP
    return (GosubS n)

returnSP :: TokParser Statement
returnSP =
    do _ <- tokenP (==ReturnTok)
       return ReturnS

onGotoSP :: TokParser Statement
onGotoSP = try $ do
    _ <- tokenP (==OnTok)
    x <- exprP
    _ <- tokenP (==GoTok)
    _ <- tokenP (==ToTok)
    ns <- sepBy1 lineNumP (tokenP (==CommaTok))
    return (OnGotoS x ns)

onGosubSP :: TokParser Statement
onGosubSP = try $ do
    _ <- tokenP (==OnTok)
    x <- exprP
    _ <- tokenP (==GoTok)
    _ <- tokenP (==SubTok)
    ns <- sepBy1 lineNumP (tokenP (==CommaTok))
    return (OnGosubS x ns)

ifSP :: TokParser Statement
ifSP =
    do _ <- tokenP (==IfTok)
       x <- exprP
       _ <- tokenP (==ThenTok)
       target <- try ifSPGoto <|> statementListP
       return (IfS x target)

ifSPGoto :: TokParser [Tagged Statement]
ifSPGoto =
    do pos <- getPosition
       n <- lineNumP
       return [Tagged pos (GotoS n)]

forSP :: TokParser Statement
forSP = do
    _ <- tokenP (==ForTok)
    vn <- varNameP
    _ <- tokenP (==EqTok)
    x1 <- exprP
    _ <- tokenP (==ToTok)
    x2 <- exprP
    x3 <- option (LitX (FloatLit 1)) (tokenP (==StepTok) >> exprP)
    return (ForS vn x1 x2 x3)

-- | Parses a @NEXT@ and an optional variable list.
nextSP :: TokParser Statement
nextSP = do
    _ <- tokenP (==NextTok)
    vns <- sepBy varNameP (tokenP (==CommaTok))
    if length vns > 0
        then return (NextS (Just vns))
        else return (NextS Nothing)

printSP :: TokParser Statement
printSP =
    do _ <- tokenP (==PrintTok)
       xs <- many printExprP
       return (PrintS xs)

printExprP :: TokParser Expr
printExprP = emptySeparatorP <|> nextZoneP <|> exprP

emptySeparatorP :: TokParser Expr
emptySeparatorP = do
    _ <- tokenP (==SemiTok)
    return EmptySeparatorX

nextZoneP :: TokParser Expr
nextZoneP = do
    _ <- tokenP (==CommaTok)
    return NextZoneX

inputSP :: TokParser Statement
inputSP =
    do _ <- tokenP (==InputTok)
       ps <- option Nothing inputPrompt
       vs <- sepBy1 varP (tokenP (==CommaTok))
       return (InputS ps vs)

inputPrompt :: TokParser (Maybe String)
inputPrompt =
    do (StringLit p) <- stringLitP
       _ <- tokenP (==SemiTok)
       return (Just p)

endSP :: TokParser Statement
endSP =
    do _ <- tokenP (==EndTok)
       return EndS

stopSP :: TokParser Statement
stopSP =
    do _ <- tokenP (==StopTok)
       return StopS

arrDeclP :: TokParser (VarName, [Expr])
arrDeclP = do
    vn <- varNameP
    xs <- argsP
    skipSpace
    return (vn, xs)

dimSP :: TokParser Statement
dimSP = do
   _ <- tokenP (==DimTok)
   arrDecls <- sepBy1 arrDeclP (tokenP (==CommaTok))
   return (DimS arrDecls)

randomizeSP :: TokParser Statement
randomizeSP = do
    _ <- tokenP (==RandomizeTok)
    return RandomizeS

readSP :: TokParser Statement
readSP = do
    _ <- tokenP (==ReadTok)
    vs <- sepBy1 varP (tokenP (==CommaTok))
    return (ReadS vs)

restoreSP :: TokParser Statement
restoreSP = do
    _ <- tokenP (==RestoreTok)
    maybeLineNum <- optionally lineNumP
    return (RestoreS maybeLineNum)

dataSP :: TokParser Statement
dataSP = do
    (Tagged _ (DataTok s)) <- tokenP isDataTok
    return (DataS s)

defFnSP :: TokParser Statement
defFnSP = do
    _ <- tokenP (==DefTok)
    _ <- tokenP (==FnTok)
    name <- varNameP
    _ <- tokenP (==LParenTok)
    params <- sepBy1 varNameP (tokenP (==CommaTok))
    _ <- tokenP (==RParenTok)
    _ <- tokenP (==EqTok)
    expr <- exprP
    return (DefFnS name params expr)

remSP :: TokParser Statement
remSP =
    do tok <- tokenP isRemTok
       return (RemS (getRemTokString (getTaggedVal tok)))

statementP :: TokParser (Tagged Statement)
statementP = do
    input <- getInput
    let pos = getPosTag (head input)
    st <- choice [printSP, inputSP, gotoSP, gosubSP, returnSP, onGotoSP, onGosubSP,
        ifSP, forSP, nextSP, endSP, stopSP, randomizeSP, dimSP, readSP, restoreSP, dataSP,
        remSP, defFnSP, letSP]
    return (Tagged pos st)

-- | Parses a list of statements from a tokenized BASIC source line.
statementListP :: TokParser [Tagged Statement]
statementListP = do
    _ <- many (tokenP (==ColonTok))
    sl <- sepEndBy1 statementP (many1 (tokenP (==ColonTok)))
    eol <?> ": OR END OF LINE"
    return sl

anyTokenP :: TokParser (Tagged Token)
anyTokenP = tokenP (const True)

eol :: TokParser ()
eol = try (do {
    tok <- anyTokenP;
    unexpected (printToken (getTaggedVal tok));
  } <|> return ()) <?> "END OF LINE"
