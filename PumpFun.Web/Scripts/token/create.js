const { VersionedTransaction, Connection, Keypair } = require('@solana/web3.js');
const bs58 = require('bs58');
const { readFile } = require('fs/promises');
const fetch = require('node-fetch');
const FormData = require('form-data');
const { basename } = require('path');
const { fileURLToPath } = require('url');

const RPC_ENDPOINT = "https://solana-api.instantnodes.io/token-XXXXX";
const web3Connection = new Connection(
    RPC_ENDPOINT,
    'confirmed',
);

/**
 * @typedef {Object} TokenCreationParams
 * @property {string} walletPrivateKey - The wallet's private key
 * @property {string} publicKey - The wallet's public key
 * @property {string} name - Token name
 * @property {string} symbol - Token symbol
 * @property {string} description - Token description
 * @property {string} imagePath - Path to token image
 * @property {string} [twitter] - Optional Twitter link
 * @property {string} [telegram] - Optional Telegram link
 * @property {string} [website] - Optional Website link
 * @property {number} [amount=0] - Initial buy amount in SOL
 * @property {number} [slippage=10] - Slippage percentage
 * @property {number} [priorityFee=0.0001] - Priority fee
 * @property {boolean} [simulation=true] - Whether to simulate the transaction
 */

async function createToken(params) {
    try {
        const signerKeyPair = Keypair.fromSecretKey(bs58.decode(params.walletPrivateKey));
        const mintKeypair = Keypair.generate();

        // Read image file
        const imageBuffer = await readFile(params.imagePath);
        
        const formData = new FormData();
        formData.append("file", imageBuffer, {
            filename: basename(params.imagePath),
            contentType: 'image/png'
        });
        formData.append("name", params.name);
        formData.append("symbol", params.symbol);
        formData.append("description", params.description);
        formData.append("twitter", params.twitter || "");
        formData.append("telegram", params.telegram || "");
        formData.append("website", params.website || "");
        formData.append("showName", "true");

        // Create IPFS metadata storage
        const metadataResponse = await fetch("https://pump.fun/api/ipfs", {
            method: "POST",
            body: formData,
        });
        const metadataResponseJSON = await metadataResponse.json();

        // Get the create transaction
        const response = await fetch(`https://pumpportal.fun/api/trade-local`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                "publicKey": params.publicKey,
                "action": "create",
                "tokenMetadata": {
                    name: metadataResponseJSON.metadata.name,
                    symbol: metadataResponseJSON.metadata.symbol,
                    uri: metadataResponseJSON.metadataUri
                },
                "mint": mintKeypair.publicKey.toBase58(),
                "denominatedInSol": "true",
                "amount": params.amount,
                "slippage": params.slippage || 10,
                "priorityFee": params.priorityFee || 0.0005,
                "pool": "pump"
            })
        });
        if(response.status === 200){ 
            const data = await response.arrayBuffer();
            const tx = VersionedTransaction.deserialize(new Uint8Array(data));
            tx.sign([mintKeypair, signerKeyPair]);

            const isSimulation = params.simulation ?? true;
            
            if (isSimulation) {
                const simulation = await web3Connection.simulateTransaction(tx);
                return {
                    success: simulation.value.err === null,
                    type: "simulation",
                    error: simulation.value.err,
                    mint: mintKeypair.publicKey.toBase58(),
                    computeUnits: simulation.value.unitsConsumed,
                    logs: simulation.value.logs
                };
            } else {
                const signature = await web3Connection.sendTransaction(tx);
                return {
                    success: true,
                    type: "transaction",
                    signature: signature,
                    mint: mintKeypair.publicKey.toBase58(),
                    explorer: `https://solscan.io/tx/${signature}`
                };
            }
        } else {
            return {
                success: false,
                type: "error",
                error: response.statusText,
                code: response.status
            };
        }
    } catch (error) {
        return {
            success: false,
            type: "error",
            error: error.message
        };
    }
}

// Handle CLI arguments
const args = process.argv[2];
if (!args) {
    console.error('No arguments provided');
    process.exit(1);
}

let params;
try {
    // Try to parse the JSON string, handling both single and double quotes
    const normalized = args.replace(/'/g, '"');
    params = JSON.parse(normalized);
} catch (error) {
    console.error('Failed to parse arguments:', error);
    process.exit(1);
}

createToken(params)
    .then(result => {
        console.log(JSON.stringify(result));
    })
    .catch(error => {
        console.error(error);
        process.exit(1);
    });