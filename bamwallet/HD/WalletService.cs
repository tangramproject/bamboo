﻿// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Newtonsoft.Json.Linq;

using Dawn;

using NBitcoin;
using NBitcoin.Stealth;

using Transaction = BAMWallet.Model.Transaction;
using Util = BAMWallet.Helper.Util;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc;
using BAMWallet.Extentions;
using BAMWallet.Services;

using Libsecp256k1Zkp.Net;

namespace BAMWallet.HD
{
    public class WalletService : IWalletService
    {
        private const string HDPath = "m/44'/847177'/0'/0/";

        private readonly ISafeguardDownloadingFlagProvider _safeguardDownloadingFlagProvider;
        private readonly ILogger _logger;
        private readonly NBitcoin.Network _network;
        private readonly Client _client;
        private readonly IConfigurationSection _apiGatewaySection;

        private ConcurrentDictionary<Guid, Session> Sessions { get; }

        public WalletService(ISafeguardDownloadingFlagProvider safeguardDownloadingFlagProvider, IConfiguration configuration, ILogger<WalletService> logger)
        {
            _safeguardDownloadingFlagProvider = safeguardDownloadingFlagProvider;

            var apiNetworkSection = configuration.GetSection(Constant.Network);
            var environment = apiNetworkSection.GetValue<string>(Constant.Environment);

            _network = environment == Constant.Mainnet ? NBitcoin.Network.Main : NBitcoin.Network.TestNet;
            _logger = logger;
            _apiGatewaySection = configuration.GetSection(RestCall.Gateway);
            _client = new Client(configuration, _logger);

            Sessions = new ConcurrentDictionary<Guid, Session>();
        }

        public Client HttpClient() => _client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Session GetSession(Guid sessionId) => Sessions.GetValueOrDefault(sessionId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Session SessionAddOrUpdate(Session session)
        {
            var mSession = Sessions.AddOrUpdate(session.SessionId, session,
                            (Key, existingVal) =>
                            {
                                if (session != existingVal)
                                    throw new ArgumentException("Duplicate sessions are not allowed: {0}.", session.SessionId.ToString());

                                existingVal.Amount = session.Amount;
                                existingVal.Memo = session.Memo;
                                existingVal.RecipientAddress = session.RecipientAddress;
                                existingVal.SenderAddress = session.SenderAddress;
                                existingVal.HasFunds = session.HasFunds;

                                return existingVal;
                            });

            return mSession;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<ulong> AvailableBalance(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            ulong balance;

            try
            {
                var session = GetSession(sessionId);

                using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                var walletTxns = db.Query<WalletTransaction>().ToList();
                if (walletTxns?.Any() != true)
                {
                    return TaskResult<ulong>.CreateSuccess(0);
                }

                balance = Balance(walletTxns, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<ulong>.CreateFailure(ex);
            }

            return TaskResult<ulong>.CreateSuccess(balance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        public void AddKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = GetSession(sessionId);

                using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                var next = LastKeySet(session.SessionId);
                var keyPath = new KeyPath(next.KeyPath);
                var index = keyPath.Indexes[3] + 1;
                var keySet = CreateKeySet(new KeyPath($"m/44'/847177'/{index}'/0/0"), next.RootKey.HexToByte(), next.ChainCode.HexToByte());

                db.Insert(keySet);

                next.ChainCode.ZeroString();
                next.RootKey.ZeroString();

                keySet.RootKey.ZeroString();
                keySet.ChainCode.ZeroString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="secretKey"></param>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        public KeySet CreateKeySet(KeyPath keyPath, byte[] secretKey, byte[] chainCode)
        {
            Guard.Argument(keyPath, nameof(keyPath)).NotNull();
            Guard.Argument(secretKey, nameof(secretKey)).NotNull().MaxCount(32);
            Guard.Argument(chainCode, nameof(chainCode)).NotNull().MaxCount(32);

            var masterKey = new ExtKey(new Key(secretKey), chainCode);
            var spend = masterKey.Derive(keyPath).PrivateKey;
            var scan = masterKey.Derive(keyPath = keyPath.Increment()).PrivateKey;

            return new KeySet
            {
                ChainCode = masterKey.ChainCode.ByteToHex(),
                KeyPath = keyPath.ToString(),
                RootKey = masterKey.PrivateKey.ToHex(),
                StealthAddress = spend.PubKey.CreateStealthAddress(scan.PubKey, _network).ToString()
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public string CreateWallet(SecureString mnemonic, SecureString passphrase)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            var walletId = NewID(16);

            walletId.MakeReadOnly();
            mnemonic.MakeReadOnly();
            passphrase.MakeReadOnly();

            CreateHDRootKey(mnemonic, passphrase, out string concatenateMnemonic, out ExtKey hdRoot);

            concatenateMnemonic.ZeroString();

            var keySet = CreateKeySet(new KeyPath($"{HDPath}0"), hdRoot.PrivateKey.ToHex().HexToByte(), hdRoot.ChainCode);

            try
            {
                using (var db = Util.LiteRepositoryFactory(passphrase, walletId.ToUnSecureString()))
                {
                    db.Insert(keySet);
                }

                keySet.ChainCode.ZeroString();
                keySet.RootKey.ZeroString();

                return walletId.ToUnSecureString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception("Failed to create wallet.");
            }
            finally
            {
                walletId.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public SecureString NewID(int bytes = 32)
        {
            using var secp256k1 = new Secp256k1();

            var secureString = new SecureString();
            foreach (var c in $"id_{secp256k1.RandomSeed(bytes).ByteToHex()}") secureString.AppendChar(c);
            return secureString;
        }

        /// <summary>
        /// BIP39 mnemonic.
        /// </summary>
        /// <returns></returns>
        public async Task<string[]> CreateMnemonic(Language language, WordCount wordCount)
        {
            var wordList = await Wordlist.LoadWordList(language);
            var mnemo = new Mnemonic(wordList, wordCount);

            return mnemo.Words;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public ulong TotalAmount(Guid sessionId, string address)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            ulong total;

            var session = GetSession(sessionId);

            using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
            {
                var txns = db.Query<WalletTransaction>().Where(x => x.Address == address).ToEnumerable();
                if (txns?.Any() != true)
                {
                    return 0;
                }

                var outputs = txns.Select(a => a.Change);
                total = Util.Sum(outputs);
            }

            return total;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public WalletTransaction LastTransaction(Guid sessionId, WalletType transactionType)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = GetSession(sessionId);

            WalletTransaction walletTx;

            using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
            {
                var transactions = db.Query<WalletTransaction>().ToList();
                if (transactions?.Any() != true)
                {
                    return null;
                }

                walletTx = transactions.Last(tx => tx.WalletType == transactionType);

                if (walletTx != null)
                {
                    var (spend, scan) = Unlock(sessionId);
                    var message = Util.DeserializeProto<WalletTransactionMessage>(scan.Decrypt(walletTx.Vout.N));

                    walletTx.Payment = message.Amount;
                    walletTx.Memo = message.Memo;
                }
            }

            return walletTx;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public TaskResult<WalletTransaction> SortChange(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            WalletTransaction walletTx = null;

            try
            {
                var session = GetSession(sessionId);

                List<WalletTransaction> transactions;
                using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
                {
                    transactions = db.Query<WalletTransaction>().ToList();
                    if (transactions?.Any() != true)
                    {
                        return TaskResult<WalletTransaction>.CreateFailure(null);
                    }
                }

                var received = transactions.Where(tx => tx.WalletType == WalletType.Receive).ToArray();
                var target = new WalletTransaction[received.Length];

                Array.Copy(received, target, received.Length);

                for (int i = 0, targetLength = target.Length; i < targetLength; i++)
                {
                    var balance = Balance(transactions, session.SessionId);
                    if (balance >= session.Amount)
                    {
                        walletTx = new WalletTransaction
                        {
                            Address = session.SenderAddress,
                            Balance = balance,
                            DateTime = DateTime.Now,
                            Payment = session.Amount,
                            Memo = session.Memo,
                            Change = balance - session.Amount,
                            Spent = balance - session.Amount == 0,
                            Id = session.SessionId,
                            Vout = received.ElementAt(i).Vout
                        };

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<WalletTransaction>.CreateFailure(ex);
            }

            return TaskResult<WalletTransaction>.CreateSuccess(walletTx);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nByte"></param>
        /// <returns></returns>
        public ulong Fee(int nByte)
        {
            return ((double)0.000012 * nByte).ConvertToUInt64();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> WalletList()
        {
            var wallets = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "wallets");
            string[] files = Directory.GetFiles(wallets, "*.db");

            if (files?.Any() != true)
            {
                return Enumerable.Empty<string>();
            }

            return files;
        }

        /// <summary>
        /// Lists all KeySets
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IEnumerable<KeySet> KeySets(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = GetSession(sessionId);

            using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

            var keys = db.Query<KeySet>().ToList();
            if (keys?.Any() != true)
            {
                return Enumerable.Empty<KeySet>();
            }

            return keys;
        }

        /// <summary>
        /// Lists all addresses
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IEnumerable<string> Addresses(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();


            var session = GetSession(sessionId);

            var keys = KeySets(session.SessionId);
            if (keys?.Any() != true)
            {
                return Enumerable.Empty<string>();
            }

            return keys.Select(k => k.StealthAddress);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public TaskResult<Transaction> CreateTransaction(Session session, WalletTransaction walletTx)
        {
            using var secp256k1 = new Secp256k1();
            using var pedersen = new Pedersen();
            using var mlsag = new MLSAG();
            using var bulletProof = new BulletProof();
            using var schnorr = new Schnorr();

            var blinds = new Span<byte[]>(new byte[4][]);
            var sk = new Span<byte[]>(new byte[2][]);
            int nRows = 2; // last row sums commitments
            int nCols = 2;//22; // ring size
            int index = Libsecp256k1Zkp.Net.Util.Rand(0, nCols) % nCols;
            var m = new byte[nRows * nCols * 33];
            var pcm_in = new Span<byte[]>(new byte[nCols * 1][]);
            var pcm_out = new Span<byte[]>(new byte[3][]);
            var randSeed = secp256k1.Randomize32();
            var preimage = secp256k1.Randomize32();
            var pc = new byte[32];
            var ki = new byte[33 * 1];
            var ss = new byte[nCols * nRows * 32];
            var blindSum = new byte[32];
            var blindSumChange = new byte[32];

            blinds[1] = pedersen.BlindSwitch(walletTx.Fee, secp256k1.CreatePrivateKey());
            blinds[2] = pedersen.BlindSwitch(walletTx.Payment, secp256k1.CreatePrivateKey());
            blinds[3] = pedersen.BlindSwitch(walletTx.Change, secp256k1.CreatePrivateKey());

            pcm_out[0] = pedersen.Commit(walletTx.Fee, blinds[1]);
            pcm_out[1] = pedersen.Commit(walletTx.Payment, blinds[2]);
            pcm_out[2] = pedersen.Commit(walletTx.Change, blinds[3]);

            m = M(session, walletTx, secp256k1, pedersen, blinds, sk, nRows, nCols, index, m, pcm_in);

            var success = mlsag.Prepare(m, blindSum, pcm_out.Length, pcm_out.Length, nCols, nRows, pcm_in, pcm_out, blinds);
            if (!success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Prepare failed."
                }));
            }

            sk[nRows - 1] = blindSum;

            success = mlsag.Generate(ki, pc, ss, randSeed, preimage, nCols, nRows, index, sk, m);
            if (!success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Generate failed."
                }));
            }

            success = mlsag.Verify(preimage, nCols, nRows, m, ki, pc, ss);
            if (!success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "MLSAG Verify failed."
                }));
            }

            blindSumChange = pedersen.BlindSum(new List<byte[]> { blinds[0] }, new List<byte[]> { blinds[1], blinds[2] });
            pcm_out[2] = pedersen.Commit(walletTx.Change, blindSumChange);

            var sumCommitBalance = pedersen.CommitSum(new List<byte[]> { pedersen.Commit(walletTx.Change, blindSumChange), pedersen.Commit(walletTx.Fee, blinds[1]), pedersen.Commit(walletTx.Payment, blinds[2]) }, new List<byte[]> { });
            if (!pedersen.VerifyCommitSum(new List<byte[]> { sumCommitBalance }, new List<byte[]> { pcm_out[2], pedersen.Commit(walletTx.Fee, blinds[1]), pedersen.Commit(walletTx.Payment, blinds[2]) }))
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Verify commit sum failed."
                }));
            }

            var bulletChange = BulletProof(walletTx.Change, blindSumChange, pcm_out[2]);
            if (!bulletChange.Success)
            {
                return TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = bulletChange.Exception.Message
                }));
            }

            var transaction = TransactionFactory(session, walletTx, nCols, m, pcm_in, pcm_out, blinds, preimage, pc, ki, ss, bulletChange.Result.proof);
            var kbOverflow = transaction.Stream().Length > 3474 + 64;

            return kbOverflow switch
            {
                true => TaskResult<Transaction>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = "Transaction overflow."
                })),
                _ => TaskResult<Transaction>.CreateSuccess(transaction)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="walletTx"></param>
        /// <param name="nCols"></param>
        /// <param name="m"></param>
        /// <param name="pcm_in"></param>
        /// <param name="pcm_out"></param>
        /// <param name="blinds"></param>
        /// <param name="preimage"></param>
        /// <param name="pc"></param>
        /// <param name="ki"></param>
        /// <param name="ss"></param>
        /// <param name="bp"></param>
        /// <returns></returns>
        private Transaction TransactionFactory(Session session, WalletTransaction walletTx, int nCols, byte[] m, Span<byte[]> pcm_in, Span<byte[]> pcm_out, Span<byte[]> blinds, byte[] preimage, byte[] pc, byte[] ki, byte[] ss, byte[] bp)
        {
            var (outPkFee, stealthFee) = MakeStealthPayment(session.SenderAddress);
            var (outPkPayment, stealthPayment) = MakeStealthPayment(session.RecipientAddress);
            var (outPkChange, stealthChange) = MakeStealthPayment(session.SenderAddress);

            var feeLockTime = new LockTime(Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddHours(21)));
            var changeLockTime = new LockTime(Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow.AddMinutes(5)));

            var tx = new Transaction
            {
                Bp = new Bp[]
                {
                    new Bp
                    {
                         Proof = bp
                    }
                },
                Mix = nCols,
                Rct = new RCT[]
                {
                    new RCT
                    {
                         I = preimage,
                         M = m,
                         P = pc,
                         S = ss
                    }
                },
                Ver = 0x1,
                Vin = new Vin[]
                {
                    new Vin
                    {
                         Key = new Aux
                         {
                              K_Image = ki,
                              K_Offsets = Offsets(pcm_in.GetEnumerator(), pcm_out.GetEnumerator(), nCols)
                         }
                    }
                },
                Vout = new Vout[]
                {
                   new Vout
                   {
                        A = walletTx.Fee,
                        C = pcm_out[1],
                        E = stealthFee.Metadata.EphemKey.ToBytes(),
                        L = feeLockTime.Value,
                        N = GetScanPublicKey(session.SenderAddress).Encrypt(Util.SerializeProto(new WalletTransactionMessage { Amount = walletTx.Fee, Blind = blinds[1], Memo = string.Empty })),
                        P = outPkFee.ToBytes(),
                        S = new Script(Op.GetPushOp(feeLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString(),
                        T = session.SessionType == SessionType.Coin ? CoinType.fee : CoinType.Coinbase
                   },
                   new Vout
                   {
                        A = session.SessionType == SessionType.Coinstake ? walletTx.Payment : 0,
                        C = pcm_out[0],
                        E = stealthPayment.Metadata.EphemKey.ToBytes(),
                        N = GetScanPublicKey(session.RecipientAddress).Encrypt(Util.SerializeProto(new WalletTransactionMessage { Amount = walletTx.Payment, Blind = blinds[2], Memo = session.Memo })),
                        P = outPkPayment.ToBytes(),
                        T = session.SessionType == SessionType.Coin ? CoinType.Coin : CoinType.Coinstake
                    },
                    new Vout
                    {
                        C = pcm_out[2],
                        E = stealthChange.Metadata.EphemKey.ToBytes(),
                        L = changeLockTime.Value,
                        N = GetScanPublicKey(session.SenderAddress).Encrypt(Util.SerializeProto(new WalletTransactionMessage { Amount = walletTx.Change, Blind = blinds[3], Memo = string.Empty })),
                        P = outPkChange.ToBytes(),
                        S = new Script(Op.GetPushOp(changeLockTime.Value), OpcodeType.OP_CHECKLOCKTIMEVERIFY).ToString(),
                        T = CoinType.Coin
                    }
                }
            };

            tx.Id = session.SessionId;
            tx.TxnId = tx.ToHash();

            walletTx.TxId = tx.TxnId;

            return tx;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="walletTx"></param>
        /// <param name="secp256k1"></param>
        /// <param name="pedersen"></param>
        /// <param name="blinds"></param>
        /// <param name="sk"></param>
        /// <param name="nRows"></param>
        /// <param name="nCols"></param>
        /// <param name="index"></param>
        /// <param name="m"></param>
        /// <param name="pcm_in"></param>
        /// <returns></returns>
        private unsafe byte[] M(Session session, WalletTransaction walletTx, Secp256k1 secp256k1, Pedersen pedersen,
            Span<byte[]> blinds, Span<byte[]> sk, int nRows, int nCols, int index, byte[] m, Span<byte[]> pcm_in)
        {
            for (int k = 0; k < nRows - 1; ++k)
                for (int i = 0; i < nCols; ++i)
                {
                    if (i == index)
                    {
                        var (spend, scan) = Unlock(session.SessionId);
                        var message = Util.DeserializeProto<WalletTransactionMessage>(scan.Decrypt(walletTx.Vout.N));
                        var oneTimeSpendKey = spend.Uncover(scan, new PubKey(walletTx.Vout.E));

                        sk[0] = oneTimeSpendKey.ToHex().HexToByte();
                        blinds[0] = pedersen.BlindSwitch(walletTx.Balance, message.Blind);

                        pcm_in[i + k * nCols] = pedersen.Commit(walletTx.Balance, blinds[0]);

                        fixed (byte* mm = m, pk = oneTimeSpendKey.PubKey.ToBytes())
                        {
                            Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                        }

                        continue;
                    }

                    // Make fake inputs. Should collect outputs as the new imputs from network.                    
                    var fakeAmountIn = 1;
                    pcm_in[i + k * nCols] = pedersen.Commit((ulong)fakeAmountIn, secp256k1.Randomize32());

                    fixed (byte* mm = m, pk = secp256k1.CreatePublicKey(secp256k1.Randomize32(), true))
                    {
                        Libsecp256k1Zkp.Net.Util.MemCpy(&mm[(i + k * nCols) * 33], pk, 33);
                    }
                }

            return m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySet"></param>
        /// <returns></returns>
        private static ExtKey GetMasterKey(KeySet keySet)
        {
            return new ExtKey(new Key(keySet.RootKey.HexToByte()), keySet.ChainCode.HexToByte());
        }

        /// <summary>
        /// Bulletproof commitment.
        /// </summary>
        /// <param name="balance"></param>
        /// <param name="blindSum"></param>
        /// <param name="commitSum"></param>
        /// <returns></returns>
        private static TaskResult<ProofStruct> BulletProof(ulong balance, byte[] blindSum, byte[] commitSum)
        {
            ProofStruct proofStruct;

            try
            {
                using var bulletProof = new BulletProof();
                using var secp256k1 = new Secp256k1();

                proofStruct = bulletProof.GenProof(balance, blindSum, secp256k1.RandomSeed(32), null, null, null);
                var success = bulletProof.Verify(commitSum, proofStruct.proof, null);

                if (!success)
                {
                    return TaskResult<ProofStruct>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Bulletproof Verify failed."
                    }));
                }
            }
            catch (Exception ex)
            {
                return TaskResult<ProofStruct>.CreateFailure(JObject.FromObject(new
                {
                    success = false,
                    message = ex.Message
                }));
            }

            return TaskResult<ProofStruct>.CreateSuccess(proofStruct);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pcin"></param>
        /// <param name="pcout"></param>
        /// <returns></returns>
        private static byte[] Offsets(Span<byte[]>.Enumerator pcin, Span<byte[]>.Enumerator pcout, int mix)
        {
            byte[] offsets = new byte[mix];
            using (var ts = new TangramStream())
            {
                while (pcin.MoveNext())
                {
                    ts.Append(pcin.Current.ToArray());
                }
                while (pcout.MoveNext())
                {
                    ts.Append(pcout.Current.ToArray());
                }
            }

            return offsets;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static KeyPath IncrementKeyPath(string path)
        {
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            var keypth = new KeyPath(path);
            return keypth.Increment();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mnemonic"></param>
        /// <param name="passphrase"></param>
        /// <param name="concatenateMnemonic"></param>
        /// <param name="hdRoot"></param>
        private static void CreateHDRootKey(SecureString mnemonic, SecureString passphrase, out string concatenateMnemonic, out ExtKey hdRoot)
        {
            Guard.Argument(mnemonic, nameof(mnemonic)).NotNull();
            Guard.Argument(passphrase, nameof(passphrase)).NotNull();

            concatenateMnemonic = string.Join(" ", mnemonic.ToUnSecureString());
            hdRoot = new Mnemonic(concatenateMnemonic).DeriveExtKey(passphrase.ToUnSecureString());
        }

        /// <summary>
        /// Calculate balance from transactions.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private ulong Balance(IEnumerable<WalletTransaction> source, Guid sessionId)
        {
            Guard.Argument(source, nameof(source)).NotNull();

            ulong? received = null;
            ulong? spent = null;
            ulong total;

            try
            {
                var (spend, scan) = Unlock(sessionId);
                foreach (var locked in source)
                {
                    var message = Util.DeserializeProto<WalletTransactionMessage>(scan.Decrypt(locked.Vout.N));
                    locked.Payment = message.Amount;
                }

                received = Sum(source, WalletType.Receive);
                spent = Sum(source, WalletType.Send);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
            finally
            {
                switch (spent)
                {
                    case null:
                        total = received == null ? 0 : received.Value;
                        break;
                    default:
                        {
                            total = received.Value - spent.Value;
                            break;
                        }
                }
            }

            return total;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        private static ulong Sum(IEnumerable<WalletTransaction> source, WalletType transactionType)
        {
            var amounts = Enumerable.Empty<ulong>();

            amounts = transactionType == WalletType.Receive
                ? source.Where(tx => tx.WalletType == transactionType).Select(p => p.Payment)
                : source.Where(tx => tx.WalletType == transactionType).Select(p => p.Balance - p.Change);

            var sum = 0UL;

            foreach (var amount in amounts)
            {
                sum += amount;
            }
            return sum;
        }

        /// <summary>
        /// Calculates the change.
        /// </summary>
        /// <returns>The change.</returns>
        /// <param name="amount">Amount.</param>
        /// <param name="transactions">Transactions.</param>
        private static (WalletTransaction, ulong) CalculateChange(ulong amount, WalletTransaction[] walletTxs)
        {
            Guard.Argument(walletTxs, nameof(walletTxs)).NotNull();

            int count;
            var tempWalletTxs = new List<WalletTransaction>();

            for (var i = 0; i < walletTxs.Length; i++)
            {
                count = (int)(amount / walletTxs[i].Change);
                if (count != 0)
                    for (int k = 0; k < count; k++) tempWalletTxs.Add(walletTxs[i]);

                amount %= walletTxs[i].Change;
            }

            var sum = Util.Sum(tempWalletTxs.Select(s => s.Change));
            var remainder = amount - sum;
            var closest = walletTxs.Select(x => x.Change).Aggregate((x, y) => x - remainder < y - remainder ? x : y);
            var walletTx = walletTxs.FirstOrDefault(a => a.Change == closest);

            return (walletTx, remainder);
        }

        /// <summary>
        /// Returns balance sheet for the calling wallet.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public IEnumerable<BlanceSheet> History(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            ulong credit = 0;
            var session = GetSession(sessionId);

            List<WalletTransaction> walletTxns;

            using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
            {
                walletTxns = db.Query<WalletTransaction>().ToList();
                if (walletTxns?.Any() != true)
                {
                    return null;
                }
            }

            var final = walletTxns.OrderBy(f => f.DateTime).Select(tx =>
            {
                var (spend, scan) = Unlock(session.SessionId);
                var message = Util.DeserializeProto<WalletTransactionMessage>(scan.Decrypt(tx.Vout.N));

                tx.Change = tx.Change == 0 ? message.Amount : tx.Change;

                return new BlanceSheet
                {
                    DateTime = tx.DateTime.ToUniversalTime(),
                    Memo = message.Memo,
                    MoneyOut = tx.WalletType == WalletType.Send ? $"-{tx.Change.DivWithNaT():F9}" : "",
                    MoneyIn = tx.WalletType == WalletType.Receive ? tx.Change.DivWithNaT().ToString("F9") : "",
                    Balance = tx.WalletType == WalletType.Send ? (credit -= tx.Change).DivWithNaT().ToString("F9") : (credit += tx.Change).DivWithNaT().ToString("F9")
                };
            });

            return final;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public int Count(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = GetSession(sessionId);

            using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

            return db.Query<WalletTransaction>().ToList().Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public KeySet LastKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            var session = GetSession(sessionId);

            using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());
            var keySet = db.Query<KeySet>().ToList().Last();

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public KeySet GetKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotZero();

            var session = GetSession(sessionId);

            using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());
            var keySet = db.Query<KeySet>().FirstOrDefault();

            session.SenderAddress = keySet.StealthAddress;

            SessionAddOrUpdate(session);

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public KeySet NextKeySet(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotZero();

            KeySet keySet = default;

            try
            {
                var session = GetSession(sessionId);

                keySet = GetKeySet(session.SessionId);

                using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                var txCount = Count(session.SessionId);
                if (txCount > 0)
                {
                    keySet.KeyPath = IncrementKeyPath(keySet.KeyPath).ToString();
                    db.Update(keySet);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return keySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="ephemKey"></param>
        /// <returns></returns>
        public (PubKey, StealthPayment) MakeStealthPayment(string address)
        {
            var ephem = new Key();
            var stealth = new BitcoinStealthAddress(address, _network);
            var payment = stealth.CreatePayment(ephem);
            var outPk = stealth.SpendPubKeys[0].UncoverSender(ephem, stealth.ScanPubKey);

            return (outPk, payment);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public PubKey GetScanPublicKey(string address)
        {
            var stealth = new BitcoinStealthAddress(address, _network);
            return stealth.ScanPubKey;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="passphrase"></param>
        /// <param name="transactionId"></param>
        /// <param name="address"></param>s
        /// <returns></returns>
        public async Task ReceivePayment(Guid sessionId, string paymentId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(paymentId, nameof(paymentId)).NotNull().NotEmpty().NotWhiteSpace();

            var session = GetSession(sessionId);

            var (spend, scan) = Unlock(session.SessionId);

            var baseAddress = _client.GetBaseAddress();
            var path = string.Format(_apiGatewaySection.GetSection(RestCall.Routing).GetValue<string>(RestCall.GetTransactionId.ToString()), paymentId);

            var vout = await _client.GetAsync<Vout>(baseAddress, path, new System.Threading.CancellationToken());
            if (vout == null)
            {
                SetLastError(session, TaskResult<Vout>.CreateFailure(new Exception($"Failed to find transaction with paymentId: {paymentId}")));
                return;
            }

            WalletTransaction walletTx = null;

            var uncover = spend.Uncover(scan, new PubKey(vout.E));
            if (uncover.PubKey.ToBytes().SequenceEqual(vout.P))
            {
                using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                var seenAddressList = db.Query<WalletTransaction>().Where(x => x.Address.Equals(session.SenderAddress)).ToEnumerable();
                var seenOutPk = seenAddressList.Any(x => x.Vout.P.SequenceEqual(vout.P));

                if (!seenOutPk)
                {
                    walletTx = new WalletTransaction
                    {
                        Address = session.SenderAddress,
                        DateTime = DateTime.UtcNow,
                        TxId = paymentId.HexToByte(),
                        Vout = vout,
                        WalletType = WalletType.Receive
                    };

                    var saved = Save(session.SessionId, walletTx);
                    if (!saved.Success)
                    {
                        throw new Exception("Could not save wallet transaction");
                    }
                }
            }

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public Task TransferPayment(Guid sessionId)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();

            try
            {
                var session = GetSession(sessionId);
                session.LastError = null;

                var balance = AvailableBalance(sessionId);
                if (!balance.Success)
                {
                    SetLastError(session, balance);
                    return Task.CompletedTask;
                }

                var spend = Spend(sessionId);
                if (!spend.Success)
                {
                    SetLastError(session, spend);
                    return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                throw;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="obj"></param>
        private void SetLastError<T>(Session session, TaskResult<T> obj)
        {
            session.LastError = JObject.FromObject(new
            {
                success = false,
                message = obj.Exception.Message
            });

            SessionAddOrUpdate(session);
            _logger.LogError(obj.Exception.Message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public (Key, Key) Unlock(Guid sessionId)
        {
            Key spend = null;
            Key scan = null;

            try
            {
                var keySet = GetKeySet(sessionId);
                var masterKey = GetMasterKey(keySet);

                spend = masterKey.Derive(new KeyPath($"{HDPath}0")).PrivateKey;
                scan = masterKey.Derive(new KeyPath($"{HDPath}1")).PrivateKey;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
            }

            return (spend, scan);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="walletTx"></param>
        /// <returns></returns>
        public TaskResult<bool> Save(Guid sessionId, WalletTransaction walletTx)
        {
            Guard.Argument(sessionId, nameof(sessionId)).NotDefault();
            Guard.Argument(walletTx, nameof(walletTx)).NotNull();

            try
            {
                var session = GetSession(sessionId);
                using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                db.Insert(walletTx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private TaskResult<bool> Spend(Guid sessionId)
        {
            try
            {
                var session = GetSession(sessionId);

                var walletTx = SortChange(sessionId);
                if (!walletTx.Success)
                {
                    return TaskResult<bool>.CreateFailure(walletTx.Exception);
                }

                var transaction = CreateTransaction(session, walletTx.Result);
                if (!transaction.Success)
                {
                    return TaskResult<bool>.CreateFailure(transaction.Exception);
                }

                using (var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString()))
                {
                    var transactionExists = db.Query<Transaction>().Where(s => s.Id.Equals(session.SessionId)).Exists();
                    if (!transactionExists)
                    {
                        db.Insert(transaction.Result);
                    }
                }

                var saved = Save(session.SessionId, walletTx.Result);
                if (!saved.Success)
                {
                    return TaskResult<bool>.CreateFailure(saved.Exception);
                }

                var baseAddress = _client.GetBaseAddress();
                var path = string.Format(_apiGatewaySection.GetSection(RestCall.Routing).GetValue<string>(RestCall.GetTransactionId.ToString()), null);

                var posted = _client.PostAsync(transaction.Result, baseAddress, path, new System.Threading.CancellationToken()).GetAwaiter().GetResult();
                if (posted == null)
                {
                    var rollBack = RollBackOne(session.SessionId);
                    if (!rollBack.Success)
                    {
                        return TaskResult<bool>.CreateFailure(new Exception($"Unable to rollback transaction with session id: {transaction.Result.Id}"));
                    }

                    return TaskResult<bool>.CreateFailure(new Exception($"Unable to send transaction with paymentId: {transaction.Result.TxnId.ByteToHex()}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                return TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private TaskResult<bool> RollBackOne(Guid sessionId)
        {
            try
            {
                var session = GetSession(sessionId);

                using var db = Util.LiteRepositoryFactory(session.Passphrase, session.Identifier.ToUnSecureString());

                var transaction = db.Query<Transaction>().Where(s => s.Equals(session.SessionId)).FirstOrDefault();
                if (transaction != null)
                {
                    db.Delete<Transaction>(new LiteDB.BsonValue(transaction.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                TaskResult<bool>.CreateFailure(ex);
            }

            return TaskResult<bool>.CreateSuccess(true);
        }
    }
}